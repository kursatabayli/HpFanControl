using Microsoft.AspNetCore.Components;
using MudBlazor;
using HpFanControl.UI.Models;
using HpFanControl.UI.Services;

namespace HpFanControl.UI.Components.Shared;

#pragma warning disable CA1515
public sealed partial class SettingsDialog : ComponentBase
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    
    [Inject] private PreferencesService PrefService { get; set; } = default!;
    [Inject] private UpdateService UpdateService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private AppPreferences _prefs = new();
    private bool _isChecking;

    protected override async Task OnInitializedAsync()
    {
        _prefs = await PrefService.GetPreferencesAsync().ConfigureAwait(false);
    }

    private async Task OnStartupCheckChanged()
    {
        await PrefService.SavePreferencesAsync(_prefs).ConfigureAwait(false);
    }

    private async Task CheckForUpdatesAsync()
    {
        _isChecking = true;
        await InvokeAsync(StateHasChanged).ConfigureAwait(false);

        bool hasUpdate = await UpdateService.CheckForUpdatesAsync(isManualCheck: true).ConfigureAwait(false);

        _isChecking = false;
        await InvokeAsync(StateHasChanged).ConfigureAwait(false);

        if (hasUpdate)
        {
            var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };
            var dialog = await DialogService.ShowAsync<UpdateDialog>("", options).ConfigureAwait(false);
            var result = await dialog.Result.ConfigureAwait(false);

            if (result is not null && !result.Canceled && result.Data is Uri response)
                    await UpdateService.InstallUpdateAsync(response).ConfigureAwait(false);
        }
        else
        {
            Snackbar.Add("You are already using the latest version.", Severity.Success);
        }
    }

    private void Close() => MudDialog.Close();
}
#pragma warning restore CA1515