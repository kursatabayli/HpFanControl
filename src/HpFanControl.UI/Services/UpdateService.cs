using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using HpFanControl.Core.Helpers;

namespace HpFanControl.UI.Services;

#pragma warning disable CA1812
internal sealed class UpdateService(IHttpClientFactory httpClientFactory, PreferencesService prefService)
{
    private const string GitHubApiName = "GitHubApi";
    private static readonly Uri LatestReleaseUri = new("releases/latest", UriKind.Relative);

    private readonly Version _currentVersion = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(1, 0, 0);

    public bool IsUpdateAvailable { get; private set; }
    public string AvailableVersion { get; private set; } = string.Empty;
    private bool _hasCheckedThisSession;

    public async Task<bool> CheckForUpdatesAsync(bool isManualCheck = false)
    {
        if (!isManualCheck && _hasCheckedThisSession)
            return IsUpdateAvailable;

        var prefs = await prefService.GetPreferencesAsync().ConfigureAwait(false);

        if (!prefs.CheckUpdatesOnStartup && !isManualCheck)
            return false;

        try
        {
            using var client = httpClientFactory.CreateClient(GitHubApiName);

            var response = await client.GetStringAsync(LatestReleaseUri).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(response);
            string latestTag = doc.RootElement.GetProperty("tag_name").GetString() ?? "";

            if (!Version.TryParse(latestTag.TrimStart('v', 'V'), out Version? latestVersion))
                return false;

            if (latestVersion > _currentVersion)
            {
                if (!isManualCheck && prefs.SkippedVersion == latestTag)
                {
                    _hasCheckedThisSession = true;
                    return false;
                }

                IsUpdateAvailable = true;
                AvailableVersion = latestTag;
                _hasCheckedThisSession = true;
                return true;
            }

            _hasCheckedThisSession = true;
            return false;
        }
        catch (HttpRequestException) { return false; }
        catch (TaskCanceledException) { return false; }
        catch (JsonException) { return false; }
    }

    public async Task SkipCurrentUpdateAsync()
    {
        if (!string.IsNullOrEmpty(AvailableVersion))
        {
            var prefs = await prefService.GetPreferencesAsync().ConfigureAwait(false);
            prefs.SkippedVersion = AvailableVersion;
            await prefService.SavePreferencesAsync(prefs).ConfigureAwait(false);

            IsUpdateAvailable = false;
        }
    }

    public async Task<(string? ReleaseNotes, Uri? DownloadUrl)> GetUpdateMetadataAsync()
    {
        using var client = httpClientFactory.CreateClient(GitHubApiName);
        var response = await client.GetStringAsync(LatestReleaseUri).ConfigureAwait(false);

        using var doc = JsonDocument.Parse(response);
        var root = doc.RootElement;

        string? releaseNotes = root.GetProperty("body").GetString();
        string urlStr = root.GetProperty("assets")[0].GetProperty("browser_download_url").GetString() ?? "";

        Uri.TryCreate(urlStr, UriKind.Absolute, out Uri? downloadUri);

        return (releaseNotes, downloadUri);
    }

    public async Task InstallUpdateAsync(Uri? downloadUrl)
    {
        if (downloadUrl == null) return;

        string tempPath = Path.GetTempPath();
        
        string tempArchive = Path.Combine(tempPath, $"{AppInfo.Name}_Update.tar.xz");
        string extractDir = Path.Combine(tempPath, $"{AppInfo.Name}_Update");

        using var client = httpClientFactory.CreateClient(GitHubApiName);
        
        using var stream = await client.GetStreamAsync(downloadUrl).ConfigureAwait(false);
        using var fileStream = new FileStream(tempArchive, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fileStream).ConfigureAwait(false);
        fileStream.Close();

        if (Directory.Exists(extractDir))
            Directory.Delete(extractDir, true);
        Directory.CreateDirectory(extractDir);

        var extractProcess = Process.Start(new ProcessStartInfo
        {
            FileName = "tar",
            Arguments = $"--strip-components=1 -xf {tempArchive} -C {extractDir}",
            UseShellExecute = false,
            CreateNoWindow = true
        });
        await extractProcess!.WaitForExitAsync().ConfigureAwait(false);

        string scriptPath = Path.Combine(extractDir, "install.sh");

        Process.Start(new ProcessStartInfo
        {
            FileName = "pkexec",
            Arguments = $"{scriptPath} --auto-update",
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = extractDir
        });
    }
}