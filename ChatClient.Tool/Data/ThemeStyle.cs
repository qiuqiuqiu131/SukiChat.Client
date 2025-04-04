using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Threading;
using ChatClient.Tool.Tools;
using Prism.Mvvm;
using SukiUI.Controls;
using SukiUI.Enums;
using Action = System.Action;

namespace ChatClient.Tool.Data;

public class ThemeStyle : BindableBase
{
    public event Action<(string, string)> ThemeStyleChanged;

    private SukiBackgroundStyle _backgroundStyle = SukiBackgroundStyle.Gradient;

    public SukiBackgroundStyle BackgroundStyle
    {
        get => _backgroundStyle;
        set
        {
            if (SetProperty(ref _backgroundStyle, value))
            {
                ThemeStyleChanged?.Invoke(("背景", value.ToString()));
                ThemeStyleTool.CurrentSukiBackgroundStyle = _backgroundStyle;
            }
        }
    }

    private bool _isLight = true;

    public bool IsLight
    {
        get => _isLight;
        set
        {
            if (SetProperty(ref _isLight, value))
            {
                ThemeStyleChanged?.Invoke(("主题", value ? "浅色" : "深色"));
                LightThemeChanged?.Invoke(value);
            }
        }
    }

    public event Action<bool> LightThemeChanged;

    private SukiColor _sukiColor = SukiColor.Blue;

    public SukiColor SukiColor
    {
        get => _sukiColor;
        set
        {
            if (SetProperty(ref _sukiColor, value))
            {
                string color = value switch
                {
                    SukiColor.Blue => "蓝色",
                    SukiColor.Green => "绿色",
                    SukiColor.Red => "红色",
                    SukiColor.Orange => "橙色",
                    _ => "蓝色"
                };
                ThemeStyleChanged?.Invoke(("主题颜色", color));
            }
        }
    }
}