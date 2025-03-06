using System.Text.Json;
using Avalonia.Collections;
using Avalonia.Styling;
using ChatClient.Tool.Data;
using ChatClient.Tool.ManagerInterface;
using SukiUI;
using SukiUI.Enums;
using SukiUI.Models;

namespace ChatClient.BaseService.Manager;

/// <summary>
/// 需要改变主题获取这个管理类即可
/// </summary>
internal class ThemeStyleManager : IThemeStyle
{
    private readonly IAppDataManager _appDataManager;

    private readonly FileInfo _themeStyleFile;
    private readonly SukiTheme _sukiTheme;

    public IAvaloniaReadOnlyList<SukiColorTheme> ColorThemes { get; init; }

    public ThemeStyle CurrentThemeStyle { get; private set; }

    public ThemeStyleManager(IAppDataManager appDataManager)
    {
        _appDataManager = appDataManager;

        _themeStyleFile = appDataManager.GetFileInfo("ThemeStyle.json");
        if (_themeStyleFile.Exists)
        {
            using (var reader = new StreamReader(_themeStyleFile.OpenRead()))
            {
                var json = reader.ReadToEnd();
                CurrentThemeStyle = JsonSerializer.Deserialize<ThemeStyle>(json) ?? new ThemeStyle();
            }
        }
        else
        {
            CurrentThemeStyle = new ThemeStyle();
            Save();
        }

        CurrentThemeStyle.ThemeStyleChanged += delegate { Save(); };
        CurrentThemeStyle.LightThemeChanged += ChangeBaseTheme;

        _sukiTheme = SukiTheme.GetInstance();
        ColorThemes = _sukiTheme.ColorThemes;

        if (!CurrentThemeStyle.IsLight)
            _sukiTheme.SwitchBaseTheme();
        _sukiTheme.ChangeColorTheme(CurrentThemeStyle.SukiColor);
    }

    public void ChangeBaseTheme(bool isLight)
    {
        CurrentThemeStyle.IsLight = isLight;
        _sukiTheme.ChangeBaseTheme(isLight ? ThemeVariant.Light : ThemeVariant.Dark);
        Save();
    }

    public void ChangeColorTheme(SukiColor sukiColor)
    {
        CurrentThemeStyle.SukiColor = sukiColor;
        _sukiTheme.ChangeColorTheme(sukiColor);
        Save();
    }

    private void Save()
    {
        using (var writer = new StreamWriter(_themeStyleFile.Create()))
        {
            var json = JsonSerializer.Serialize(CurrentThemeStyle);
            writer.Write(json);
        }
    }
}