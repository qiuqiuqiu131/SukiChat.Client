using Avalonia.Collections;
using ChatClient.Tool.Data;
using SukiUI.Enums;
using SukiUI.Models;

namespace ChatClient.Tool.ManagerInterface;

public interface IThemeStyle
{
    ThemeStyle CurrentThemeStyle { get; }
    IAvaloniaReadOnlyList<SukiColorTheme> ColorThemes { get; }
    void ChangeColorTheme(SukiColor sukiColor);
}