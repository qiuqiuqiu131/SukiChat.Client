using System;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using ChatClient.Tool.Data;

namespace ChatClient.Desktop.Views.Login;

public partial class LoginView : UserControl, IDisposable
{
    public LoginView()
    {
        InitializeComponent();

        IDBox.ValueMemberBinding = new Binding("ID");
        IDBox.ItemFilter = (searchText, item) =>
        {
            if (item is LoginUserItem loginUserItem && !string.IsNullOrEmpty(searchText))
                return loginUserItem.ID.StartsWith(searchText, StringComparison.OrdinalIgnoreCase);
            return false;
        };
    }

    #region Dispose

    private bool _isDisposed = false;

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                if (DataContext is IDisposable disposable)
                    disposable.Dispose();
            }

            // 释放非托管资源（如果有的话）

            _isDisposed = true;
        }
    }

    /// <summary>
    /// 析构函数，作为安全网
    /// </summary>
    ~LoginView()
    {
        Dispose(false);
    }

    #endregion

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            if (button.ContextMenu != null)
            {
                if (button.ContextMenu.IsOpen)
                    button.ContextMenu.Close();
                else
                    button.ContextMenu.Open();
            }
        }
    }
}