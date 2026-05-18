using System;
using System.IO;
using System.Text.Json;

namespace FH6Mod.Services;

/// <summary>
/// Persistent app preferences, JSON-serialized to %APPDATA%/ChaarkorFH6Mod/settings.json.
/// Survives restarts. Static singleton because UI behaviors (stagger entrance etc.) need
/// access without DI plumbing.
/// </summary>
public sealed class AppSettings
{
    public bool AnimationsEnabled { get; set; } = true;
    public int  AnimationStaggerMs { get; set; } = 60;     // per-card delay
    public int  AnimationDurationMs { get; set; } = 320;   // per-card duration

    public static AppSettings Current { get; private set; } = LoadOrDefault();

    private static readonly string SettingsDir  = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ChaarkorFH6Mod");
    private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

    private static AppSettings LoadOrDefault()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded != null) return loaded;
            }
        }
        catch { /* corrupted settings → fall through to defaults */ }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch { /* disk full / no perms → silent fail, runtime still works */ }
    }

    public event Action? Changed;
    public void NotifyChanged() { Changed?.Invoke(); Save(); }
}
