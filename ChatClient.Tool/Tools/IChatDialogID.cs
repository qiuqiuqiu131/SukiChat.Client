using ChatClient.Tool.HelperInterface;

namespace ChatClient.Tool.Tools;

public interface IChatDialogID
{
    public string ID { get; }
    public FileTarget FileTarget { get; }
}