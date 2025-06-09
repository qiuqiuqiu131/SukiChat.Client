using Material.Icons;
using Prism.Mvvm;

namespace ChatClient.Desktop.Tool;

public class NaviBar(string pageName, MaterialIconKind icon, MaterialIconKind iconOutline, string regionName)
    : BindableBase
{
    public string PageName { get; private set; } = pageName;
    public MaterialIconKind Icon { get; private set; } = icon;
    public MaterialIconKind IconOutline { get; private set; } = iconOutline;
    public string RegionName { get; private set; } = regionName;

    private int unReadMessageCount = 0;

    public int? UnReadMessageCount
    {
        get => unReadMessageCount == 0 ? null : unReadMessageCount;
        set => SetProperty(ref unReadMessageCount, value ?? 0);
    }

    private bool note;

    public bool Note
    {
        get => note;
        set => SetProperty(ref note, value);
    }
}