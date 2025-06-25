using Avalonia.Controls;
using Avalonia.Input;

namespace ChatClient.Avalonia.Controls.CustomSelectableTextBlock;

public class CustomSelectableTextBlock : SelectableTextBlock
{
    private ContextMenu? _contextMenu;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (!CanCopy)
        {
            if (_contextMenu == null && ContextMenu != null)
                _contextMenu = ContextMenu;
            ContextMenu = null;
        }
        else
            ContextMenu = _contextMenu;

        base.OnPointerPressed(e);
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            if (!CanCopy)
                e.Handled = false;
        }
    }
}