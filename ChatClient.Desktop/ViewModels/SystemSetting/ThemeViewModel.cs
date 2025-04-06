using System;
using System.Linq;
using Avalonia.Collections;
using ChatClient.Tool.Data;
using ChatClient.Tool.ManagerInterface;
using Material.Icons;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using SukiUI.Enums;
using SukiUI.Models;

namespace ChatClient.Desktop.ViewModels.SystemSetting;

public class ThemeViewModel : BindableBase, IRegionMemberLifetime
{
    public IAvaloniaReadOnlyList<SukiColorTheme> AvailableColors { get; }
    public IAvaloniaReadOnlyList<SukiBackgroundStyle> AvailableBackgroundStyles { get; }

    private ThemeStyle _themeStyle;

    public ThemeStyle ThemeStyle
    {
        get => _themeStyle;
        set => SetProperty(ref _themeStyle, value);
    }

    private bool _isLight;

    public bool IsLight
    {
        get => _isLight;
        set
        {
            if (SetProperty(ref _isLight, value))
            {
                ThemeStyle.IsLight = value;
            }
        }
    }

    private SukiColorTheme _selectedColorTheme;

    public SukiColorTheme SelectedColorTheme
    {
        get => _selectedColorTheme;
        set => SetProperty(ref _selectedColorTheme, value);
    }

    public DelegateCommand<SukiColorTheme> SwitchToColorThemeCommand { get; }

    private readonly IThemeStyle _themeStyleManager;

    public ThemeViewModel(IThemeStyle themeStyleManager)
    {
        this._themeStyleManager = themeStyleManager;

        AvailableBackgroundStyles = new AvaloniaList<SukiBackgroundStyle>(Enum.GetValues<SukiBackgroundStyle>());
        AvailableColors = _themeStyleManager.ColorThemes;

        ThemeStyle = themeStyleManager.CurrentThemeStyle;
        IsLight = ThemeStyle.IsLight;
        SelectedColorTheme =
            AvailableColors.FirstOrDefault(d => d.DisplayName.Equals(ThemeStyle.SukiColor.ToString()))!;

        SwitchToColorThemeCommand = new DelegateCommand<SukiColorTheme>(SwitchToColorTheme);
    }

    private void SwitchToColorTheme(SukiColorTheme colorTheme)
    {
        if (SelectedColorTheme == colorTheme)
            return;

        SelectedColorTheme = colorTheme;
        var targetColor = colorTheme.DisplayName switch
        {
            "Blue" => SukiColor.Blue,
            "Green" => SukiColor.Green,
            "Red" => SukiColor.Red,
            "Orange" => SukiColor.Orange,
            _ => SukiColor.Blue
        };
        _themeStyleManager.ChangeColorTheme(targetColor);
    }

    public bool KeepAlive { get; } = false;
}