using Material.Icons;

namespace ChatClient.Tool.Common;

public class ChatPageBase(string displayName, MaterialIconKind icon, int index) : ValidateBindableBase
{
    public string DisplayName
    {
        get => displayName;
        set => SetProperty(ref displayName, value);
    }

    private int unReadMessageCount;

    public int UnReadMessageCount
    {
        get => unReadMessageCount;
        set => SetProperty(ref unReadMessageCount, value);
    }


    public MaterialIconKind Icon
    {
        get => icon;
        set => SetProperty(ref icon, value);
    }

    public int Index
    {
        get => index;
        set => SetProperty(ref index, value);
    }

    public virtual void OnNavigatedTo(INavigationParameters? parameters = null)
    {
    }

    public virtual void OnNavigatedFrom()
    {
    }
}