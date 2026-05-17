using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FH6Mod.Services;

/// <summary>
/// Monitors Forza Horizon 6 process: attach/detach lifecycle, exposes PID + base address.
/// Polled every 2s on a background timer.
/// </summary>
public sealed class GameProcessService : IDisposable
{
    public const string ProcessName = "ForzaHorizon6";

    private readonly Timer _poll;
    private Process? _process;

    public event Action? StatusChanged;

    public bool IsAttached => _process is { HasExited: false };
    public int? Pid => IsAttached ? _process!.Id : null;
    public IntPtr BaseAddress
    {
        get
        {
            // Steam build allows managed MainModule; UWP build throws AccessDenied.
            // We just need a value for the status bar — fall back to zero rather than crash the UI.
            try { return IsAttached && _process!.MainModule is { } m ? m.BaseAddress : IntPtr.Zero; }
            catch { return IntPtr.Zero; }
        }
    }
    public long ModuleSize
    {
        get
        {
            try { return IsAttached && _process!.MainModule is { } m ? m.ModuleMemorySize : 0; }
            catch { return 0; }
        }
    }

    public GameProcessService()
    {
        _poll = new Timer(_ => Poll(), null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
    }

    private void Poll()
    {
        try
        {
            var was = IsAttached;
            if (_process is { HasExited: false })
                return;

            _process = Process.GetProcessesByName(ProcessName).FirstOrDefault();
            var nowAttached = IsAttached;
            if (was != nowAttached)
                StatusChanged?.Invoke();
        }
        catch
        {
            // process queries can race during exit; swallow and try next tick
        }
    }

    public void Dispose() => _poll.Dispose();
}
