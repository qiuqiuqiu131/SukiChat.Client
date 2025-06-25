using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Prism.Dialogs;

namespace ChatClient.Desktop.CornerDialog;

public interface ICornerDialogWindow
{
    /// <summary>Dialog content.</summary>
    object Content { get; set; }

    /// <summary>The window's owner.</summary>
    /// <remarks>Avalonia's WindowBase.Owner's property access is { get; protected set; }.</remarks>
    WindowBase Owner { get; }

    /// <summary>Show a non-modal dialog.</summary>
    void Show();

    /// <summary>Show a modal dialog.</summary>
    /// <returns></returns>
    Task ShowDialog(Window owner);

    /// <summary>
    /// The data context of the window.
    /// </summary>
    /// <remarks>
    /// The data context must implement <see cref="IDialogAware"/>.
    /// </remarks>
    object DataContext { get; set; }

    /// <summary>Called when the window is loaded.</summary>
    /// <remarks>
    ///     Avalonia currently doesn't implement the Loaded event like WPF.
    ///     Window > WindowBase > TopLevel.Opened
    ///     Window > WindowBase > TopLevel > Control > InputElement > Interactive > layout > Visual > StyledElement.Initialized
    /// </remarks>
    event EventHandler Opened;

    /// <summary>
    /// Called when the window is closed.
    /// </summary>
    event EventHandler Closed;

    /// <summary>
    /// Called when the window is closing.
    /// </summary>
    // WPF: event CancelEventHandler Closing;
    event EventHandler<WindowClosingEventArgs>? Closing;

    /// <summary>
    /// The result of the dialog.
    /// </summary>
    IDialogResult Result { get; set; }

    /// <summary>
    /// Called before the window is closed.
    /// </summary>
    void BeforeClose();

    void BeforeCloseByService();

    double Width { get; set; }

    double Height { get; set; }

    PixelPoint Position { get; set; }
}