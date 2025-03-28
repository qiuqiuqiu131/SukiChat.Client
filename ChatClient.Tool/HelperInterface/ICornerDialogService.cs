namespace ChatClient.Tool.HelperInterface;

public interface ICornerDialogService
{
    void ShowDialog(string name, IDialogParameters parameters, DialogCallback callback);
}