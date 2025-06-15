using Avalonia;
using SukiUI.Controls;
using SukiUI.Enums;

namespace ChatClient.Avalonia.Extenstions;

public static class SukiDialogExtensions
{
    public static readonly AttachedProperty<bool> BackgroundAnimationProperty =
        AvaloniaProperty.RegisterAttached<SukiDialog, bool>(
            "BackgroundAnimation", typeof(SukiDialog), defaultValue: false);

    public static void SetBackgroundAnimation(SukiDialog dialog, bool value)
    {
        dialog.SetValue(BackgroundAnimationProperty, value);
    }

    public static bool GetBackgroundAnimation(SukiDialog dialog)
    {
        return dialog.GetValue(BackgroundAnimationProperty);
    }


    public static readonly AttachedProperty<SukiBackgroundStyle> BackgroundStyleProperty =
        AvaloniaProperty.RegisterAttached<SukiDialog, SukiBackgroundStyle>("BackgroundStyle", typeof(SukiDialog),
            defaultValue: SukiBackgroundStyle.Gradient);

    public static void SetBackgroundStyle(SukiDialog obj, SukiBackgroundStyle value)
    {
        obj.SetValue(BackgroundStyleProperty, value);
    }

    public static SukiBackgroundStyle GetBackgroundStyle(SukiDialog obj)
    {
        return obj.GetValue(BackgroundStyleProperty);
    }


    public static readonly AttachedProperty<bool> BackgroundTransitionsEnabledProperty =
        AvaloniaProperty.RegisterAttached<SukiDialog, bool>(
            "BackgroundTransitionsEnabled", typeof(SukiDialog), defaultValue: false);

    public static void SetBackgroundTransitionsEnabled(SukiDialog dialog, bool value)
    {
        dialog.SetValue(BackgroundTransitionsEnabledProperty, value);
    }

    public static bool GetBackgroundTransitionsEnabled(SukiDialog dialog)
    {
        return dialog.GetValue(BackgroundTransitionsEnabledProperty);
    }

    public static readonly AttachedProperty<double> BackgroundTransitionTimeProperty =
        AvaloniaProperty.RegisterAttached<SukiDialog, double>(
            "BackgroundTransitionTime", typeof(SukiDialog), defaultValue: 0);

    public static void SetBackgroundTransitionTime(SukiDialog dialog, double value)
    {
        dialog.SetValue(BackgroundTransitionTimeProperty, value);
    }

    public static double GetBackgroundTransitionTime(SukiDialog dialog)
    {
        return dialog.GetValue(BackgroundTransitionTimeProperty);
    }
}