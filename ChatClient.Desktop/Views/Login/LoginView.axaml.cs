using System;
using Avalonia.Controls;
using Prism.Navigation;

namespace ChatClient.Desktop.Views.Login;

public partial class LoginView : UserControl, IDisposable
{
    public LoginView()
    {
        InitializeComponent();
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
}