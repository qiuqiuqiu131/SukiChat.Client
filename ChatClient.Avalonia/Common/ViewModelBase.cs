namespace ChatClient.Tool.Common;

public abstract class ViewModelBase : BindableBase, INavigationAware
{
    private string _title;
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    /// <summary>
    ///   Handles Prism's request to navigate to.
    ///   Don't call this directly, use OnNavigatingTo
    ///   to comply with Prism v8.x
    /// </summary>
    /// <param name="navigationContext">Navigation Context.</param>
    /// <returns>Return True to allow navigation, False to deny it.</returns>
    public bool IsNavigationTarget(NavigationContext navigationContext)
    {
        // Auto-allow navigation
        return OnNavigatingTo(navigationContext);
    }

    /// <summary>Perform any (event) cleanup, we're navigating away.</summary>
    /// <param name="navigationContext">Navigation parameters.</param>
    public virtual void OnNavigatedFrom(NavigationContext navigationContext)
    {
    }

    /// <summary>Navigated to view.</summary>
    /// <param name="navigationContext">Navigation parameters.</param>
    public virtual void OnNavigatedTo(NavigationContext navigationContext)
    {
    }

    public virtual bool OnNavigatingTo(NavigationContext navigationContext)
    {
        return true;
    }
}