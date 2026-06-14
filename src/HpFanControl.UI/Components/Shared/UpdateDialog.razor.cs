using Microsoft.AspNetCore.Components;
using MudBlazor;
using HpFanControl.UI.Services;

namespace HpFanControl.UI.Components.Shared;

#pragma warning disable CA1515
public sealed partial class UpdateDialog : ComponentBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Inject] private UpdateService UpdateService { get; set; } = default!;

    private bool _isLoading = true;
    private string _releaseNotes = string.Empty;
    private Uri? _downloadUrl;
    private string _version = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        _version = UpdateService.AvailableVersion;
        
        var (ReleaseNotes, DownloadUrl) = await UpdateService.GetUpdateMetadataAsync().ConfigureAwait(false);
        
        _releaseNotes = ReleaseNotes;
        _downloadUrl = DownloadUrl;
        _isLoading = false;
    }

    private void Cancel() => MudDialog.Cancel();
    private void Update() => MudDialog.Close(DialogResult.Ok(_downloadUrl));
}
#pragma warning restore CA1515