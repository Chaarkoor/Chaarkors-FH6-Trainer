using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FH6Mod.Cheats.RuntimeHook;

namespace FH6Mod.Cheats.Sql;

/// <summary>
/// Executes SQL queries on the game's in-memory <c>CDatabase</c> by injecting
/// shellcode that calls <c>ExecuteQuery</c> (vtable index 9) via <c>CreateRemoteThread</c>.
///
/// Ported 1:1 from Autoshow Unlocker v1.3.0 — including the multi-AOB candidate
/// resolution and the 34-byte shellcode template.
/// </summary>
public sealed class SqlExecutor
{
    // Six AOB candidates autoshow tries (FH6 has shipped slightly different paths
    // across builds — the first that yields a valid CDatabase + executable
    // QueryFunction wins).
    private static readonly string[] DbSigs =
    [
        "48 8B 0D ? ? ? ? 48 8B 01 4C 8D 45 ? 48 8D 55 ? FF 50 48 90 48 8B 4D ? 48 85 C9",
        "0F 84 ? ? ? ? 48 8B 35 ? ? ? ? 48 85 F6 74",
        "0F 85 ? ? ? ? 48 8B 35 ? ? ? ? 48 85 F6 74",
        "48 8B 35 ? ? ? ? 48 85 F6 74",
        "48 8B 35 ? ? ? ? 48 85 F6 0F 84",
        "48 8B 35 ? ? ? ? 48 85 F6 0F 85",
    ];

    private readonly RuntimeHookEngine _engine;
    private readonly Dictionary<SqlFeature, CancellationTokenSource> _locks = new();
    private readonly object _locksMutex = new();
    private DatabaseCandidate _candidate;

    public bool IsResolved => _candidate.Valid;
    public ulong DatabaseObject => _candidate.DatabaseObject;
    public ulong QueryFunction => _candidate.QueryFunction;

    public SqlExecutor(RuntimeHookEngine engine) => _engine = engine;

    public void Reset()
    {
        StopAllLocks();
        _candidate = default;
    }

    // ===== Periodic SQL lock (e.g. Free Cars stays at price 0 even after game reloads) =====

    public bool IsLockActive(SqlFeature f)
    {
        lock (_locksMutex) return _locks.ContainsKey(f);
    }

    /// <summary>
    /// Starts a background timer that re-applies the given SQL queries every <paramref name="periodSec"/>
    /// seconds. Use for features whose value the game restores from save/disk periodically.
    /// </summary>
    public bool StartLock(SqlFeature f, string[] queries, int periodSec, out string? error)
    {
        error = null;
        if (!EnsureResolved(out error)) return false;
        lock (_locksMutex)
        {
            if (_locks.ContainsKey(f))
            {
                _engine.LogPublic($"{f}: lock already active.");
                return true;
            }
            // Run once immediately to confirm
            foreach (var q in queries)
                if (!Execute(q, out var qErr)) { error = qErr; return false; }

            var cts = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    try { await Task.Delay(periodSec * 1000, cts.Token); }
                    catch (OperationCanceledException) { return; }
                    if (!_engine.IsAttached) continue;
                    foreach (var q in queries)
                        Execute(q, out _);
                }
            }, cts.Token);
            _locks[f] = cts;
            _engine.LogPublic($"{f}: lock ARMED, re-apply every {periodSec}s.");
        }
        return true;
    }

    public bool StopLock(SqlFeature f, string[]? revertQueries, out string? error)
    {
        error = null;
        lock (_locksMutex)
        {
            if (!_locks.Remove(f, out var cts))
            {
                _engine.LogPublic($"{f}: lock already OFF.");
                return true;
            }
            cts.Cancel();
            cts.Dispose();
        }
        if (revertQueries == null || revertQueries.Length == 0)
        {
            _engine.LogPublic($"{f}: lock DISARMED (no revert).");
            return true;
        }
        if (!_engine.IsAttached)
        {
            _engine.LogPublic($"{f}: lock DISARMED — not attached, skipping revert.");
            return true;
        }
        foreach (var q in revertQueries)
            if (!Execute(q, out var qErr)) { error = qErr; return false; }
        _engine.LogPublic($"{f}: lock DISARMED + reverted from backup.");
        return true;
    }

    public void StopAllLocks()
    {
        lock (_locksMutex)
        {
            foreach (var (_, t) in _locks) t.Dispose();
            _locks.Clear();
        }
    }

    public bool EnsureResolved(out string? error)
    {
        error = null;
        if (_candidate.Valid) return true;
        if (!_engine.IsAttached) { error = "Not attached to FH6."; return false; }

        var moduleBytes = _engine.ReadBytesPublic(_engine.MainBase, _engine.MainSize);
        if (moduleBytes.Length == 0) { error = "Could not read main module."; return false; }

        foreach (var sig in DbSigs)
        {
            foreach (var off in Pattern.FindAll(moduleBytes, Pattern.Parse(sig), 32))
            {
                var cand = TryBuildCandidate(moduleBytes, _engine.MainBase, off);
                if (!cand.Valid) continue;
                _candidate = cand;
                _engine.LogPublic(
                    $"SQL DB resolved via '{sig.Substring(0, Math.Min(30, sig.Length))}...' @ 0x{cand.MatchAddress:X} " +
                    $"db=0x{cand.DatabaseObject:X} fn=0x{cand.QueryFunction:X}");
                return true;
            }
        }

        error = "CDatabase signature not found (FH6 likely patched the entry).";
        _engine.LogPublic("SQL DB resolution FAILED — none of the 6 AOB candidates matched.");
        return false;
    }

    public bool Execute(string sql, out string? error)
    {
        error = null;
        if (!EnsureResolved(out error)) return false;
        try
        {
            var resultPtr = ExecuteOnCandidate(sql);
            _engine.LogPublic($"SQL exec OK db=0x{_candidate.DatabaseObject:X}: {Truncate(sql, 120)}");
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            _engine.LogPublic($"SQL exec FAILED: {ex.Message}");
            return false;
        }
    }

    // ===== internal =====

    private struct DatabaseCandidate
    {
        public bool Valid;
        public ulong MatchAddress;
        public ulong PointerAddress;
        public ulong DatabaseObject;
        public ulong QueryFunction;
    }

    private DatabaseCandidate TryBuildCandidate(byte[] moduleBytes, ulong moduleBase, int matchOffset)
    {
        // Walk up to 24 bytes forward looking for `mov rcx,[rip+disp32]` or `mov rsi,[rip+disp32]`
        // i.e. 48 8B 0D ? ? ? ?  or  48 8B 35 ? ? ? ?
        for (var i = 0; i < 24 && matchOffset + i + 7 < moduleBytes.Length; i++)
        {
            if (moduleBytes[matchOffset + i] != 0x48 ||
                moduleBytes[matchOffset + i + 1] != 0x8B ||
                (moduleBytes[matchOffset + i + 2] != 0x35 && moduleBytes[matchOffset + i + 2] != 0x0D))
                continue;

            var disp = BitConverter.ToInt32(moduleBytes, matchOffset + i + 3);
            var nextInstr = (long)moduleBase + matchOffset + i + 7;
            var pointerAddr = (ulong)(nextInstr + disp);

            var dbObject = _engine.ReadUInt64Public(pointerAddr);
            if (dbObject == 0) continue;

            var vtable = _engine.ReadUInt64Public(dbObject);
            if (vtable == 0) continue;

            // vtable[9] = vtable + 9*8 = vtable + 72 = ExecuteQuery function ptr
            var queryFn = _engine.ReadUInt64Public(vtable + 72);
            if (queryFn == 0 || !_engine.IsExecutableAddressPublic(queryFn)) continue;

            return new DatabaseCandidate
            {
                Valid = true,
                MatchAddress = moduleBase + (ulong)matchOffset,
                PointerAddress = pointerAddr,
                DatabaseObject = dbObject,
                QueryFunction = queryFn,
            };
        }
        return default;
    }

    private ulong ExecuteOnCandidate(string sql)
    {
        var sqlBytes = Encoding.ASCII.GetBytes(sql + "\0");
        var handle = _engine.HandlePublic;

        var sqlMem    = Native.VirtualAllocEx(handle, IntPtr.Zero, (UIntPtr)(ulong)Math.Max(4096, sqlBytes.Length), Native.MEM_COMMIT | Native.MEM_RESERVE, Native.PAGE_READWRITE);
        var resultMem = Native.VirtualAllocEx(handle, IntPtr.Zero, (UIntPtr)8u, Native.MEM_COMMIT | Native.MEM_RESERVE, Native.PAGE_READWRITE);
        var codeMem   = Native.VirtualAllocEx(handle, IntPtr.Zero, (UIntPtr)4096u, Native.MEM_COMMIT | Native.MEM_RESERVE, Native.PAGE_EXECUTE_READWRITE);

        if (sqlMem == IntPtr.Zero || resultMem == IntPtr.Zero || codeMem == IntPtr.Zero)
            throw new InvalidOperationException("VirtualAllocEx failed during SQL execute.");

        try
        {
            _engine.WriteBytesPublic((ulong)sqlMem.ToInt64(), sqlBytes);
            _engine.WriteBytesPublic((ulong)resultMem.ToInt64(), new byte[8]);

            var shellcode = BuildQueryShellcode(
                (ulong)resultMem.ToInt64(),
                (ulong)sqlMem.ToInt64(),
                _candidate.QueryFunction);

            _engine.WriteBytesPublic((ulong)codeMem.ToInt64(), shellcode);

            var thread = CreateRemoteThread(handle, IntPtr.Zero, 0u, codeMem,
                new IntPtr((long)_candidate.DatabaseObject), 0u, out _);
            if (thread == IntPtr.Zero) throw new InvalidOperationException("CreateRemoteThread failed.");

            var wait = WaitForSingleObject(thread, 15000u);
            GetExitCodeThread(thread, out var exit);
            Native.CloseHandle(thread);
            if (wait == 258) throw new InvalidOperationException("Remote SQL thread timed out (>15s).");
            if (exit != 0) _engine.LogPublic($"SQL thread exit 0x{exit:X}");

            return _engine.ReadUInt64Public((ulong)resultMem.ToInt64());
        }
        finally
        {
            Native.VirtualFreeEx(handle, codeMem,   UIntPtr.Zero, Native.MEM_RELEASE);
            Native.VirtualFreeEx(handle, sqlMem,    UIntPtr.Zero, Native.MEM_RELEASE);
            Native.VirtualFreeEx(handle, resultMem, UIntPtr.Zero, Native.MEM_RELEASE);
        }
    }

    /// <summary>
    /// 34-byte shellcode template:
    ///   [00] 48 BA  resultPtr             mov rdx, imm64
    ///   [10] 49 B8  sqlPtr                mov r8,  imm64
    ///   [20] FF 25 00 00 00 00            jmp [rip+0]
    ///   [26]       functionPtr            (target stored inline, dereferenced by FF 25)
    /// rcx (this/CDatabase) comes from CreateRemoteThread lpParameter.
    /// </summary>
    private static byte[] BuildQueryShellcode(ulong resultPtr, ulong sqlPtr, ulong functionPtr)
    {
        byte[] code =
        [
            0x48, 0xBA, 0,0,0,0,0,0,0,0,
            0x49, 0xB8, 0,0,0,0,0,0,0,0,
            0xFF, 0x25, 0,0,0,0,
            0,0,0,0,0,0,0,0,
        ];
        Buffer.BlockCopy(BitConverter.GetBytes(resultPtr),   0, code, 2,  8);
        Buffer.BlockCopy(BitConverter.GetBytes(sqlPtr),      0, code, 12, 8);
        Buffer.BlockCopy(BitConverter.GetBytes(functionPtr), 0, code, code.Length - 8, 8);
        return code;
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s.Substring(0, max) + "…";

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes,
        uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out uint lpThreadId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetExitCodeThread(IntPtr hThread, out uint lpExitCode);
}
