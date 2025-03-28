using Prism.Dialogs;

namespace ChatClient.Desktop.Tool;

public static class ICornerDialogWindowExtensions
{
    /// <summary>
    /// Get the <see cref="IDialogAware"/> ViewModel from a <see cref="IDialogWindow"/>.
    /// </summary>
    /// <param name="dialogWindow"><see cref="IDialogWindow"/> to get ViewModel from.</param>
    /// <returns>ViewModel as a <see cref="IDialogAware"/>.</returns>
    internal static IDialogAware GetDialogViewModel(this ICornerDialogWindow dialogWindow)
    {
        return (IDialogAware)dialogWindow.DataContext;
    }
}