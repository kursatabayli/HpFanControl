using HpFanControl.Core.Models;
using MudBlazor;

namespace HpFanControl.UI.Models;

internal sealed record ModeUiDefinition(FanMode Id, string Label, string Icon, Color ThemeColor);