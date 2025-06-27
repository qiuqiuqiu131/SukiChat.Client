using Avalonia.Collections;
using SukiUI.Enums;
using SukiUI.Models;

namespace ChatClient.Desktop.Suki;

public interface IThemeStyle
{
    ThemeStyle CurrentThemeStyle { get; }
    IAvaloniaReadOnlyList<SukiColorTheme> ColorThemes { get; }
    void ChangeColorTheme(SukiColor sukiColor);
}