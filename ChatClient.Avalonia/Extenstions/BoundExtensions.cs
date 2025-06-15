using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;

namespace ChatClient.Avalonia.Extenstions;

public class BoundExtensions
{
    public static readonly AttachedProperty<bool> BoundsChangedProperty =
        AvaloniaProperty.RegisterAttached<BoundExtensions, Control, bool>("BoundsChanged");

    // 获取附加属性的值
    public static bool GetBoundsChanged(Control window)
    {
        return window.GetValue(BoundsChangedProperty);
    }

    // 设置附加属性的值
    public static void SetBoundsChanged(Control window, bool value)
    {
        window.SetValue(BoundsChangedProperty, value);
    }

    // 触发变更通知的方法
    public static void NotifyBoundsChanged(Control window)
    {
        SetBoundsChanged(window, !GetBoundsChanged(window)); // 切换值以触发 PropertyChanged
    }
}