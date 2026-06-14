using Microsoft.AspNetCore.Components;
using HpFanControl.UI.Services;
using MudBlazor;

namespace HpFanControl.UI.Components.Shared;

#pragma warning disable CA1515
public sealed partial class UpdateNotification : ComponentBase
{
    [Inject] private UpdateService UpdateService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;

    private bool _isUpdateAvailable;

    protected override async Task OnInitializedAsync()
    {
        bool hasUpdate = await UpdateService.CheckForUpdatesAsync().ConfigureAwait(false);

        if (hasUpdate)
        {
            _isUpdateAvailable = true;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
    }

    private async Task HandleUpdateClick()
    {
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };

        var dialog = await DialogService.ShowAsync<UpdateDialog>("", options).ConfigureAwait(false);
        var result = await dialog.Result.ConfigureAwait(false);

        if (result is not null && !result.Canceled && result.Data is Uri response)
            await UpdateService.InstallUpdateAsync(response).ConfigureAwait(false);
    }

    private async Task HandleDismissClick()
    {
        _isUpdateAvailable = false;
        await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        await UpdateService.SkipCurrentUpdateAsync().ConfigureAwait(false);
    }
}
#pragma warning restore CA1515