namespace ChatClient.Android.Shared.Tool;

public interface IBackHandle
{
    bool CanHandleBack();
    void HandleBack();
}