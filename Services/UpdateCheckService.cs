using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace FH6Mod.Services;

/// <summary>
/// Minimal GitHub release polling: 1 HTTP GET on startup → compare semver → expose result.
/// No auth (public repo, 60 req/h per IP anonymous limit, single fetch per launch).
/// </summary>
public sealed class UpdateCheckService
{
    private const string LatestUrl = "https://api.github.com/repos/Chaarkoor/Chaarkors-FH6-Trainer/releases/latest";
    public const string ReleasesUrl = "https://github.com/Chaarkoor/Chaarkors-FH6-Trainer/releases";
    private const string UserAgent  = "ChaarkorFH6Mod-Updater";

    public event Action? StateChanged;

    public Version CurrentVersion { get; }
    public Version? LatestVersion { get; private set; }
    public string? LatestTag { get; private set; }
    public bool IsUpdateAvailable => LatestVersion != null && LatestVersion > CurrentVersion;
    public bool HasChecked { get; private set; }
    public string? LastError { get; private set; }

    public UpdateCheckService()
    {
        var v = Assembly.GetExecutingAssembly().GetName().Version;
        CurrentVersion = v ?? new Version(0, 0, 0);
    }

    /// <summary>
    /// Single fire-and-forget check. Call once at app startup. Failures are silent
    /// (logged into LastError) — never bother the user about network/GitHub issues.
    /// </summary>
    public void CheckInBackground()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
                http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(UserAgent, CurrentVersion.ToString()));
                http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

                using var response = await http.GetAsync(LatestUrl, CancellationToken.None);
                response.EnsureSuccessStatusCode();
                await using var stream = await response.Content.ReadAsStreamAsync();
                using var doc = await JsonDocument.ParseAsync(stream);

                var tag = doc.RootElement.GetProperty("tag_name").GetString();
                if (string.IsNullOrWhiteSpace(tag)) return;

                LatestTag = tag;
                LatestVersion = ParseSemver(tag);
                HasChecked = true;
                LastError = null;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                HasChecked = true;
            }
            finally
            {
                Dispatcher.UIThread.Post(() => StateChanged?.Invoke());
            }
        });
    }

    private static Version? ParseSemver(string tag)
    {
        // Accept "v0.2.0", "0.2.0", "v0.2.0-beta" — strip leading 'v' and pre-release suffix
        var s = tag.TrimStart('v', 'V');
        var dash = s.IndexOf('-');
        if (dash > 0) s = s[..dash];
        return Version.TryParse(s, out var v) ? v : null;
    }
}
