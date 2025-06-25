using Avalonia.Media.Imaging;

namespace ChatClient.Tool.Data;

public class LoginUserItem : BindableBase
{
    public string ID { get; set; }
    public int HeadIndex { get; set; }

    public DateTime LastLoginTime { get; set; }

    private Bitmap? head = null;

    public Bitmap? Head
    {
        get => head;
        set => SetProperty(ref head, value);
    }
}