using System;
using FH6Mod.Cheats.RuntimeHook;
using FH6Mod.Cheats.Sql;

namespace FH6Mod.Services;

/// <summary>
/// Wraps the RuntimeHookEngine and the GameProcessService.
/// Re-attaches when the game starts; auto-detaches when the game exits/crashes
/// so we never write into a dead process.
/// </summary>
public sealed class CheatService : IDisposable
{
    private readonly GameProcessService _game;
    private readonly RuntimeHookEngine _engine = new();
    private readonly SqlExecutor _sql;
    private readonly System.Collections.Generic.HashSet<RuntimeProfileFeature> _active = new();
    private int _lastAttachedPid;

    public string? LastError { get; private set; }
    public string Diagnostics => _engine.DiagnosticsTail();

    public bool IsAttached => _engine.IsAttached;
    public bool IsActive(RuntimeProfileFeature f) => _active.Contains(f);

    public CheatService(GameProcessService game)
    {
        _game = game;
        _sql = new SqlExecutor(_engine);
        _game.StatusChanged += OnGameStatusChanged;
    }

    public void Dispose()
    {
        _game.StatusChanged -= OnGameStatusChanged;
        _engine.Dispose();
    }

    private void OnGameStatusChanged()
    {
        if (!_game.IsAttached && _engine.IsAttached)
        {
            // Game just died/exited — clean up our hooks so the next attach starts fresh
            _active.Clear();
            _sql.Reset();
            try { _engine.Detach(); }
            catch (Exception ex) { LastError = $"Detach on game-exit failed: {ex.Message}"; }
        }
    }

    public bool RunSql(SqlFeature feature)
    {
        if (!EnsureAttached()) return false;
        var f = SqlFeatureCatalog.Get(feature);
        foreach (var q in f.Queries)
        {
            if (!_sql.Execute(q, out var err))
            {
                LastError = $"{f.Name}: {err}";
                return false;
            }
        }
        LastError = null;
        return true;
    }

    public bool EnsureAttached()
    {
        if (!_game.IsAttached)
        {
            LastError = "Forza Horizon 6 is not running.";
            return false;
        }
        if (_engine.IsAttached && _lastAttachedPid == _game.Pid) return true;

        // If we were attached to an old/dead PID, detach first
        if (_engine.IsAttached) { _engine.Detach(); _active.Clear(); }

        if (!_engine.Attach(_game.Pid!.Value))
        {
            LastError = "OpenProcess failed (need admin? or game still loading?).";
            return false;
        }
        _lastAttachedPid = _game.Pid!.Value;
        LastError = null;
        return true;
    }

    public bool Apply(RuntimeProfileFeature feature, int value, bool enabled)
    {
        if (!EnsureAttached()) return false;
        if (!_engine.ApplyProfile(feature, value, enabled, out var err))
        {
            LastError = err;
            return false;
        }
        if (enabled) _active.Add(feature);
        else _active.Remove(feature);
        LastError = null;
        return true;
    }

    public bool UpdateValue(RuntimeProfileFeature feature, int value)
    {
        if (!EnsureAttached()) return false;
        if (!_engine.UpdateValue(feature, value, out var err))
        {
            LastError = err;
            return false;
        }
        LastError = null;
        return true;
    }

    /// <summary>
    /// Toggle a "locked" SQL feature: starts/stops a background timer that re-applies
    /// the feature SQL periodically so the game can't restore it from save. On disable
    /// it tries to revert from the _backup_* tables that were created at lock time.
    /// </summary>
    public bool ToggleSqlLock(SqlFeature feature, bool on, int periodSec = 10)
    {
        if (!EnsureAttached()) return false;
        var f = SqlFeatureCatalog.Get(feature);
        var revert = SqlFeatureCatalog.GetRevert(feature);
        var ok = on
            ? _sql.StartLock(feature, f.Queries, periodSec, out var err)
            : _sql.StopLock(feature, revert, out err);
        if (!ok) LastError = $"{f.Name}: {err}";
        else LastError = null;
        return ok;
    }

    public bool IsSqlLockActive(SqlFeature feature) => _sql.IsLockActive(feature);
}
