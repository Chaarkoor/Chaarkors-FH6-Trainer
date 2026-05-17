using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace FH6Mod.Cheats.RuntimeHook;

/// <summary>
/// Direct port of the Autoshow Unlocker v1.3.0 runtime hook engine.
/// Owns the FH6 process handle, the CRC bypass arming, and installs/removes
/// per-feature function detours. All offsets and ASM bytes match v1.3.0.
/// </summary>
public sealed class RuntimeHookEngine : IDisposable
{
    private readonly object _lock = new();
    private readonly Dictionary<string, RuntimeDetour> _hooks = new(StringComparer.OrdinalIgnoreCase);

    private IntPtr _handle;
    private Process? _process;
    private ulong _mainBase;
    private int _mainSize;
    private bool _crcBypassActive;
    private ulong _crcFunctionPointerAddress;
    private ulong _crcOriginalPointer;
    private ulong _crcRetAddress;
    private Timer? _crcTimer;
    private int _crcTimerRunning;

    public bool IsAttached => _handle != IntPtr.Zero && _process is { HasExited: false };
    public List<string> Log { get; } = new();

    // ===== Public surface for sibling subsystems (e.g. SqlExecutor) =====
    public IntPtr HandlePublic => _handle;
    public ulong  MainBase     => _mainBase;
    public int    MainSize     => _mainSize;
    public byte[] ReadBytesPublic(ulong addr, int len) => ReadBytes(addr, len);
    public ulong  ReadUInt64Public(ulong addr)         => ReadUInt64(addr);
    public void   WriteBytesPublic(ulong addr, byte[] data) => WriteBytes(addr, data);
    public bool   IsExecutableAddressPublic(ulong addr) => IsExecutableAddress(addr);
    public void   LogPublic(string msg) => L(msg);

    public string DiagnosticsTail(int lines = 12)
        => string.Join("\n", Log.Skip(Math.Max(0, Log.Count - lines)));

    private void L(string msg)
    {
        lock (_lock) Log.Add(msg);
    }

    // ===== Attach =====

    public bool Attach(int pid)
    {
        Native.EnableDebugPrivilege();
        var h = Native.OpenProcess(Native.PROCESS_ALL_ACCESS, false, (uint)pid);
        if (h == IntPtr.Zero)
        {
            L($"OpenProcess({pid}) failed.");
            return false;
        }

        Process p;
        try { p = Process.GetProcessById(pid); }
        catch (Exception ex) { Native.CloseHandle(h); L($"GetProcessById failed: {ex.Message}"); return false; }

        // Try managed MainModule first (fast path for Steam build)
        try
        {
            var m = p.MainModule!;
            _handle = h;
            _process = p;
            _mainBase = (ulong)m.BaseAddress.ToInt64();
            _mainSize = m.ModuleMemorySize;
            L($"Attached PID {pid} (managed path). base=0x{_mainBase:X}, size={_mainSize}B, file={m.FileName}");
            return true;
        }
        catch (Exception managedEx)
        {
            // UWP / sandboxed processes throw AccessDenied here — fall back to Win32 EnumProcessModulesEx
            L($"MainModule denied (likely UWP/Xbox build) — falling back to native EnumProcessModulesEx. Detail: {managedEx.Message}");
        }

        var found = Native.FindMainModule(h, "ForzaHorizon6");
        if (found is null)
        {
            Native.CloseHandle(h);
            L("Native EnumProcessModulesEx also failed — cannot locate ForzaHorizon6 main module. Are you running as admin?");
            return false;
        }

        _handle = h;
        _process = p;
        _mainBase = (ulong)found.Value.Base.ToInt64();
        _mainSize = (int)found.Value.Size;
        L($"Attached PID {pid} (UWP fallback). base=0x{_mainBase:X}, size={_mainSize}B, file={found.Value.Path}");
        return true;
    }

    /// <summary>
    /// Cleanly detach: restore hook bytes, free caves, restore CRC pointer,
    /// stop timer, close process handle.
    /// </summary>
    public void Detach()
    {
        StopCrcTimer();
        RestoreRuntimeProfileHooks();
        RestoreCrcPointer();

        _process?.Dispose();
        _process = null;
        if (_handle != IntPtr.Zero) Native.CloseHandle(_handle);
        _handle = IntPtr.Zero;
        _mainBase = 0;
        _mainSize = 0;
        _crcBypassActive = false;
    }

    public void Dispose() => Detach();

    private void StopCrcTimer()
    {
        var t = _crcTimer;
        _crcTimer = null;
        try { t?.Dispose(); } catch { }
    }

    private void RestoreRuntimeProfileHooks()
    {
        lock (_lock)
        {
            foreach (var det in _hooks.Values)
            {
                try
                {
                    if (_handle != IntPtr.Zero)
                    {
                        WriteProtectedBytes(det.Address, det.Original);
                        if (det.DetourAddress != 0)
                            Native.VirtualFreeEx(_handle, new IntPtr((long)det.DetourAddress), UIntPtr.Zero, Native.MEM_RELEASE);
                    }
                }
                catch (Exception ex) { L($"Could not restore {det.Name}: {ex.Message}"); }
            }
            if (_hooks.Count > 0) L($"Restored {_hooks.Count} runtime hook(s).");
            _hooks.Clear();
        }
    }

    private void RestoreCrcPointer()
    {
        if (!_crcBypassActive || _crcFunctionPointerAddress == 0 || _crcOriginalPointer == 0 || _handle == IntPtr.Zero)
            return;
        try { WriteUInt64(_crcFunctionPointerAddress, _crcOriginalPointer); }
        catch (Exception ex) { L($"CRC pointer restore failed: {ex.Message}"); }
        _crcBypassActive = false;
    }

    // ===== Profile hooks (Credits / Wheelspins / SP / Drift / NoSkillBreak / Sell) =====

    public bool ApplyProfile(RuntimeProfileFeature feature, int value, bool enabled, out string? error)
    {
        error = null;
        if (!IsAttached) { error = "Not attached."; return false; }
        var desc = ProfileFeatureCatalog.Get(feature);
        try
        {
            RuntimeDetour det;
            lock (_lock)
            {
                if (!enabled)
                {
                    if (!_hooks.TryGetValue(desc.Key, out det!))
                    {
                        L($"{desc.Name} hook already OFF.");
                        return true;
                    }
                }
                else
                {
                    det = EnsureProfileHook(desc);
                }
            }
            WriteByte(det.DetourAddress + (ulong)desc.ToggleOffset, (byte)(enabled ? 1 : 0));
            if (desc.ValueOffset >= 0)
                WriteInt32(det.DetourAddress + (ulong)desc.ValueOffset, value);
            L($"{desc.Name} {(enabled ? "ENABLED" : "DISABLED")} @ detour 0x{det.DetourAddress:X}, value={value}.");
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            L($"{desc.Name} apply failed: {ex.Message}");
            return false;
        }
    }

    public bool UpdateValue(RuntimeProfileFeature feature, int value, out string? error)
    {
        error = null;
        var desc = ProfileFeatureCatalog.Get(feature);
        lock (_lock)
        {
            if (!_hooks.TryGetValue(desc.Key, out var det))
            {
                error = $"{desc.Name} is not enabled.";
                return false;
            }
            if (desc.ValueOffset < 0)
            {
                error = $"{desc.Name} does not accept a value.";
                return false;
            }
            try
            {
                WriteInt32(det.DetourAddress + (ulong)desc.ValueOffset, value);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }

    private RuntimeDetour EnsureProfileHook(RuntimeProfileHookDescriptor desc)
    {
        if (_hooks.TryGetValue(desc.Key, out var existing)) return existing;

        EnsureCrcBypass();

        var moduleBytes = ReadBytes(_mainBase, _mainSize);
        if (moduleBytes.Length == 0)
            throw new InvalidOperationException($"Could not read main module for {desc.Name} scan.");

        var off = FindFirstPatternOffset(moduleBytes, desc.Signature);
        if (off < 0)
            throw new InvalidOperationException($"{desc.Name} signature was not found.\nSig: {desc.Signature}");

        ulong hookAddr;
        if (desc.ResolveCallTarget)
        {
            var callAddr = _mainBase + (ulong)off;
            var head = ReadBytes(callAddr, 5);
            if (head.Length < 5 || head[0] != 0xE8)
                throw new InvalidOperationException($"{desc.Name} sig did not resolve to a CALL instruction.");
            var rel = BitConverter.ToInt32(head, 1);
            hookAddr = (ulong)((long)(callAddr + 5) + rel + desc.CallTargetOffset);
        }
        else
        {
            hookAddr = (ulong)((long)_mainBase + off + desc.MatchOffset);
        }

        var original = ReadBytes(hookAddr, desc.HookSize);
        if (original.Length < desc.HookSize)
            throw new InvalidOperationException($"Could not read {desc.Name} hook target at 0x{hookAddr:X}.");

        if (!BytesStartWith(original, desc.ExpectedOriginal))
        {
            if (original.Length > 0 && original[0] == 0xE9)
                throw new InvalidOperationException($"{desc.Name} hook target already patched by another tool. Close other trainers and retry.");
            throw new InvalidOperationException($"{desc.Name} hook target bytes mismatch (FH6 may have updated). Found: {FormatBytes(original)}");
        }

        var det = CreateRuntimeDetour(desc, hookAddr);
        _hooks[desc.Key] = det;
        L($"{desc.Name} detour installed. target=0x{hookAddr:X}, detour=0x{det.DetourAddress:X}");
        return det;
    }

    private RuntimeDetour CreateRuntimeDetour(RuntimeProfileHookDescriptor desc, ulong hookAddr)
    {
        var original = ReadBytes(hookAddr, desc.HookSize);
        var caveSize = Math.Max(
            desc.Asm.Length + 5,
            Math.Max(desc.ToggleOffset + 1, desc.ValueOffset >= 0 ? desc.ValueOffset + 4 : 0));

        var caveAddr = AllocateNear(hookAddr, caveSize);
        var cave = new byte[caveSize];
        Buffer.BlockCopy(desc.Asm, 0, cave, 0, desc.Asm.Length);
        var jmpBack = BuildRelativeJump(caveAddr + (ulong)desc.Asm.Length, hookAddr + (ulong)desc.HookSize, 5);
        Buffer.BlockCopy(jmpBack, 0, cave, desc.Asm.Length, jmpBack.Length);
        WriteBytes(caveAddr, cave);

        var hookPatch = BuildRelativeJump(hookAddr, caveAddr, desc.HookSize);
        WriteProtectedBytes(hookAddr, hookPatch);

        return new RuntimeDetour
        {
            Name = desc.Name,
            Address = hookAddr,
            DetourAddress = caveAddr,
            Size = caveSize,
            Original = original,
            Patch = hookPatch,
        };
    }

    // ===== CRC bypass + heartbeat re-arm =====

    private void EnsureCrcBypass()
    {
        if (_crcBypassActive) return;
        if (_mainBase == 0 || _mainSize <= 0)
            throw new InvalidOperationException("Main module not captured.");

        var bytes = ReadBytes(_mainBase, _mainSize);
        if (bytes.Length == 0) throw new InvalidOperationException("Could not read main module for CRC bypass.");

        var retOff = FindFirstExecutablePatternOffset(bytes, "C3");
        if (retOff < 0) throw new InvalidOperationException("CRC bypass ret-stub not found.");

        var crcOff = FindFirstPatternOffset(bytes, "48 8B D9 48 8D 05 ? ? ? ? 48 89 01 E8 ? ? ? ? 48 8B CB 48 83 C4 20 5B E9");
        if (crcOff < 0) throw new InvalidOperationException("CRC bypass signature not found (FH6 likely updated).");

        var sigAddr = _mainBase + (ulong)crcOff;
        var leaStart = sigAddr + 3;                                   // skip 48 8B D9
        var leaDisp = ReadInt32(leaStart + 3);                        // 48 8D 05 <disp32>
        var tableBase = leaStart + 7 + (ulong)leaDisp;
        var fnPtrAddr = tableBase + 48;
        var origFnPtr = ReadUInt64(fnPtrAddr);
        if (origFnPtr == 0) throw new InvalidOperationException("CRC function pointer is zero.");
        var retAddr = _mainBase + (ulong)retOff;

        WriteUInt64(fnPtrAddr, retAddr);
        _crcFunctionPointerAddress = fnPtrAddr;
        _crcOriginalPointer = origFnPtr;
        _crcRetAddress = retAddr;
        _crcBypassActive = true;
        StartCrcTimer();
        L($"CRC bypass armed. ptr=0x{fnPtrAddr:X}, ret=0x{retAddr:X}");
    }

    private void StartCrcTimer()
    {
        _crcTimer ??= new Timer(CrcTimerTick, null, 10_000, 10_000);
    }

    /// <summary>
    /// Authentic autoshow v1.3.0 dance:
    /// 1) Restore originals (hooks + CRC pointer) — give the game ~1s to run its own
    ///    integrity checks and pass them cleanly.
    /// 2) Re-apply patches.
    /// Without this window the game's own CRC self-check destabilizes or aborts
    /// because it can never see legitimate code.
    /// </summary>
    private void CrcTimerTick(object? _)
    {
        if (Interlocked.Exchange(ref _crcTimerRunning, 1) == 1) return;
        try
        {
            // Phase 1: show originals
            lock (_lock)
            {
                if (!_crcBypassActive || _handle == IntPtr.Zero || _process?.HasExited != false) return;
                try
                {
                    foreach (var det in _hooks.Values)
                        WriteProtectedBytes(det.Address, det.Original);
                    WriteUInt64(_crcFunctionPointerAddress, _crcOriginalPointer);
                }
                catch (Exception ex) { L($"CRC phase-1 (restore) failed: {ex.Message}"); return; }
            }

            // Give the game a window to verify cleanly
            Thread.Sleep(1000);

            // Phase 2: re-apply patches
            lock (_lock)
            {
                if (!_crcBypassActive || _handle == IntPtr.Zero || _process?.HasExited != false) return;
                try
                {
                    WriteUInt64(_crcFunctionPointerAddress, _crcRetAddress);
                    foreach (var det in _hooks.Values)
                        WriteProtectedBytes(det.Address, det.Patch);
                }
                catch (Exception ex) { L($"CRC phase-2 (re-apply) failed: {ex.Message}"); }
            }
        }
        catch (Exception ex) { L($"CRC tick uncaught: {ex.Message}"); }
        finally { Interlocked.Exchange(ref _crcTimerRunning, 0); }
    }

    // ===== low-level read/write/alloc =====

    private byte[] ReadBytes(ulong address, int length)
    {
        if (length <= 0) return [];
        var buf = new byte[length];
        if (!Native.ReadProcessMemory(_handle, new IntPtr((long)address), buf, (UIntPtr)(ulong)length, out var read))
            return [];
        var got = (int)(uint)read;
        if (got == length) return buf;
        if (got <= 0) return [];
        var trimmed = new byte[got];
        Buffer.BlockCopy(buf, 0, trimmed, 0, got);
        return trimmed;
    }

    private ulong ReadUInt64(ulong address)
    {
        var b = ReadBytes(address, 8);
        return b.Length < 8 ? 0UL : BitConverter.ToUInt64(b, 0);
    }

    private int ReadInt32(ulong address)
    {
        var b = ReadBytes(address, 4);
        return b.Length < 4 ? 0 : BitConverter.ToInt32(b, 0);
    }

    private void WriteBytes(ulong address, byte[] data)
    {
        if (!Native.WriteProcessMemory(_handle, new IntPtr((long)address), data, (UIntPtr)(ulong)data.Length, out var written)
            || (ulong)written != (ulong)data.Length)
            throw new InvalidOperationException($"WriteProcessMemory @ 0x{address:X} failed.");
    }

    private void WriteByte(ulong address, byte value) => WriteBytes(address, [value]);
    private void WriteInt32(ulong address, int value) => WriteBytes(address, BitConverter.GetBytes(value));
    private void WriteUInt64(ulong address, ulong value) => WriteProtectedBytes(address, BitConverter.GetBytes(value));

    private void WriteProtectedBytes(ulong address, byte[] data)
    {
        if (!Native.VirtualProtectEx(_handle, new IntPtr((long)address), (UIntPtr)(ulong)data.Length,
                Native.PAGE_EXECUTE_READWRITE, out var old))
            throw new InvalidOperationException("VirtualProtectEx failed.");
        try { WriteBytes(address, data); }
        finally { Native.VirtualProtectEx(_handle, new IntPtr((long)address), (UIntPtr)(ulong)data.Length, old, out _); }
    }

    private ulong AllocateNear(ulong target, int size)
    {
        var page = target & 0xFFFF_FFFF_FFFF_0000UL;
        for (ulong step = 0; step <= 0x7000_0000UL; step += 0x1_0000UL)
        {
            if (page > step)
            {
                var r = TryAllocateAt(page - step, size, target);
                if (r != 0) return r;
            }
            var up = page + step;
            if (up < 0x0000_7FFF_FFFE_0000UL)
            {
                var r = TryAllocateAt(up, size, target);
                if (r != 0) return r;
            }
        }
        throw new InvalidOperationException($"Could not allocate detour near 0x{target:X}.");
    }

    private ulong TryAllocateAt(ulong address, int size, ulong target)
    {
        if (address == 0) return 0;
        var p = Native.VirtualAllocEx(_handle, new IntPtr((long)address),
            (UIntPtr)(ulong)Math.Max(size, 4096),
            Native.MEM_COMMIT | Native.MEM_RESERVE,
            Native.PAGE_EXECUTE_READWRITE);
        if (p == IntPtr.Zero) return 0;
        var got = (ulong)p.ToInt64();
        if (RelativeJumpFits(target, got) && RelativeJumpFits(got, target)) return got;
        Native.VirtualFreeEx(_handle, p, UIntPtr.Zero, Native.MEM_RELEASE);
        return 0;
    }

    // ===== pattern + jump helpers =====

    private int FindFirstPatternOffset(byte[] data, string sig)
    {
        var pat = Pattern.Parse(sig);
        foreach (var o in Pattern.FindAll(data, pat, 1)) return o;
        return -1;
    }

    private int FindFirstExecutablePatternOffset(byte[] data, string sig)
    {
        var pat = Pattern.Parse(sig);
        foreach (var o in Pattern.FindAll(data, pat, 512))
        {
            if (IsExecutableAddress(_mainBase + (ulong)o)) return o;
        }
        return -1;
    }

    private bool IsExecutableAddress(ulong addr)
    {
        if (Native.VirtualQueryEx(_handle, (UIntPtr)addr, out var mbi,
                (UIntPtr)(ulong)System.Runtime.InteropServices.Marshal.SizeOf<Native.MemoryBasicInformation64>()) == UIntPtr.Zero)
            return false;
        return Native.IsExecutable(mbi.Protect);
    }

    private static byte[] BuildRelativeJump(ulong from, ulong to, int length)
    {
        if (length < 5) throw new InvalidOperationException("Jump length < 5.");
        var diff = (long)(to - (from + 5));
        if (diff < int.MinValue || diff > int.MaxValue)
            throw new InvalidOperationException("Jump out of int32 range.");
        var arr = new byte[length];
        arr[0] = 0xE9;
        Buffer.BlockCopy(BitConverter.GetBytes((int)diff), 0, arr, 1, 4);
        for (var i = 5; i < arr.Length; i++) arr[i] = 0x90;
        return arr;
    }

    private static bool RelativeJumpFits(ulong from, ulong to)
    {
        var d = (long)(to - (from + 5));
        return d >= int.MinValue && d <= int.MaxValue;
    }

    private static bool BytesStartWith(byte[] current, byte[] expected)
    {
        if (expected.Length == 0) return true;
        if (current.Length < expected.Length) return false;
        for (var i = 0; i < expected.Length; i++)
            if (current[i] != expected[i]) return false;
        return true;
    }

    private static string FormatBytes(byte[] b) => string.Join(" ", b.Select(x => x.ToString("X2")));
}
