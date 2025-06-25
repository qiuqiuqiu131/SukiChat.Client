using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using ChatClient.Tool.HelperInterface;
using Prism.Common;
using Prism.Dialogs;
using Prism.Ioc;
using Prism.Mvvm;

namespace ChatClient.Desktop.CornerDialog;

public class CornerDialogService : ICornerDialogService
{
    private readonly IContainerProvider _containerProvider;

    public CornerDialogService(IContainerProvider containerProvider)
    {
        _containerProvider = containerProvider;
    }

    /// <summary>
    /// 显示边角对话框
    /// </summary>
    /// <param name="name"></param>
    /// <param name="parameters">
    /// ShowNonModal：是否显示非模态窗口
    /// WindowName：注册的window的name
    /// ParentWindow：父窗口
    /// </param>
    /// <param name="delay"></param>
    public void ShowDialog(string name, IDialogParameters parameters, DialogCallback callback)
    {
        parameters ??= new DialogParameters();
        var isModal = parameters.TryGetValue<bool>(KnownDialogParameters.ShowNonModal, out var show) ? !show : true;
        var windowName = parameters.TryGetValue<string>(KnownDialogParameters.WindowName, out var wName) ? wName : null;
        var owner = parameters.TryGetValue<Window>(KnownDialogParameters.ParentWindow, out var hWnd) ? hWnd : null;

        // Create a new dialog window
        ICornerDialogWindow dialogWindow = CreateCornerDialogWindow(windowName);
        ConfigureDialogWindowEvents(dialogWindow, callback);
        ConfigureDialogWindowContent(name, dialogWindow, parameters);

        ShowDialogWindow(dialogWindow, isModal, owner);
    }

    /// <summary>Shows the dialog window.</summary>
    /// <param name="dialogWindow">The dialog window to show.</param>
    /// <param name="isModal">If true; dialog is shown as a modal</param>
    /// <param name="owner">Optional host window of the dialog. Use-case, Dialog calling a dialog.</param>
    protected virtual void ShowDialogWindow(ICornerDialogWindow dialogWindow, bool isModal, Window owner = null)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime deskLifetime)
        {
            foreach (var window in deskLifetime.Windows)
            {
                if (window is ICornerDialogWindow cornerDialogWindow && cornerDialogWindow != dialogWindow)
                    cornerDialogWindow.BeforeCloseByService();
            }

            if (isModal)
            {
                if (owner != null)
                    dialogWindow.ShowDialog(owner);
                else
                    dialogWindow.ShowDialog(deskLifetime.MainWindow);
            }
            else
            {
                dialogWindow.Show();
            }
        }
    }

    /// <summary>
    /// Create a new <see cref="ICornerDialogWindow"/>.
    /// </summary>
    /// <param name="name">The name of the hosting window registered with the IContainerRegistry.</param>
    /// <returns>The created <see cref="ICornerDialogWindow"/>.</returns>
    protected virtual ICornerDialogWindow CreateCornerDialogWindow(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return _containerProvider.Resolve<ICornerDialogWindow>();
        else
            return _containerProvider.Resolve<ICornerDialogWindow>(name);
    }

    /// <summary>
    /// Configure <see cref="ICornerDialogWindow"/> content.
    /// </summary>
    /// <param name="dialogName">The name of the dialog to show.</param>
    /// <param name="window">The hosting window.</param>
    /// <param name="parameters">The parameters to pass to the dialog.</param>
    protected virtual void ConfigureDialogWindowContent(string dialogName, ICornerDialogWindow window,
        IDialogParameters parameters)
    {
        var content = _containerProvider.Resolve<object>(dialogName);
        if (!(content is Control dialogContent))
            throw new NullReferenceException("A dialog's content must be an Avalonia.Controls.Control");

        // MvvmHelpers.AutowireViewModel(dialogContent);
        if (dialogContent is Control view &&
            view.DataContext is null &&
            ViewModelLocator.GetAutoWireViewModel(view) is null)
        {
            ViewModelLocator.SetAutoWireViewModel(view, true);
        }

        if (!(dialogContent.DataContext is IDialogAware viewModel))
            throw new NullReferenceException(
                "A dialog's ViewModel must implement the Prism.Dialogs.IDialogAware interface");

        ConfigureDialogWindowProperties(window, dialogContent, viewModel);

        // Call OnDialogOpened
        MvvmHelpers.ViewAndViewModelAction<IDialogAware>(viewModel, d => d.OnDialogOpened(parameters));
    }

    /// <summary>
    /// Configure <see cref="ICornerDialogWindow"/> and <see cref="IDialogAware"/> events.
    /// </summary>
    /// <param name="dialogWindow">The hosting window.</param>
    /// <param name="callback">The action to perform when the dialog is closed.</param>
    protected virtual void ConfigureDialogWindowEvents(ICornerDialogWindow dialogWindow, DialogCallback callback)
    {
        Action<IDialogResult> requestCloseHandler = (result) =>
        {
            dialogWindow.Result = result;
            dialogWindow.BeforeClose();
        };

        EventHandler loadedHandler = null;

        loadedHandler = (o, e) =>
        {
            dialogWindow.Opened -= loadedHandler;
            DialogUtilities.InitializeListener(dialogWindow.GetDialogViewModel(), requestCloseHandler);

            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var ds = desktop.MainWindow.DesktopScaling;
                var workingArea = desktop.MainWindow?.Screens?.Primary?.WorkingArea;
                if (workingArea != null)
                {
                    var x = workingArea.Value.X + workingArea.Value.Width - dialogWindow.Width * ds;
                    var y = workingArea.Value.Y + workingArea.Value.Height - dialogWindow.Height * ds;

                    dialogWindow.Position = new PixelPoint((int)(x), (int)(y));
                }
            }
        };

        dialogWindow.Opened += loadedHandler;

        EventHandler<WindowClosingEventArgs> closingHandler = null;
        closingHandler = (o, e) =>
        {
            if (!dialogWindow.GetDialogViewModel().CanCloseDialog())
                e.Cancel = true;
        };

        dialogWindow.Closing += closingHandler;

        EventHandler closedHandler = null;
        closedHandler = async (o, e) =>
        {
            dialogWindow.Closed -= closedHandler;
            dialogWindow.Closing -= closingHandler;

            dialogWindow.GetDialogViewModel().OnDialogClosed();

            if (dialogWindow.Result == null)
                dialogWindow.Result = new DialogResult();

            await callback.Invoke(dialogWindow.Result);

            dialogWindow.DataContext = null;
            dialogWindow.Content = null;
        };

        dialogWindow.Closed += closedHandler;
    }

    /// <summary>
    /// Configure <see cref="ICornerDialogWindow"/> properties.
    /// </summary>
    /// <param name="window">The hosting window.</param>
    /// <param name="dialogContent">The dialog to show.</param>
    /// <param name="viewModel">The dialog's ViewModel.</param>
    protected virtual void ConfigureDialogWindowProperties(ICornerDialogWindow window,
        Control dialogContent, IDialogAware viewModel)
    {
        // Make the host window and the dialog window to share the same context
        window.Content = dialogContent;
        window.DataContext = viewModel;
    }
}