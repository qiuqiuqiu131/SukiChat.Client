using Material.Icons;

namespace ChatClient.Tool.Common;

public class ChatPageBase(string displayName, MaterialIconKind icon, int index) : ValidateBindableBase
{
    private string _displayName = displayName;

    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    private MaterialIconKind _icon = icon;

    public MaterialIconKind Icon
    {
        get => _icon;
        set => SetProperty(ref _icon, value);
    }

    private int _index = index;

    public int Index
    {
        get => _index;
        set => SetProperty(ref _index, value);
    }

    public virtual void OnNavigatedTo(INavigationParameters? parameters = null)
    {
    }

    public virtual void OnNavigatedFrom()
    {
    }
}