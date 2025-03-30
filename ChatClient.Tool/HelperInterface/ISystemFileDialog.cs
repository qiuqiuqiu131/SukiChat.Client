namespace ChatClient.Tool.HelperInterface;

public interface ISystemFileDialog
{
    Task<string> OpenFileAsync(IntPtr ownerHandle, string windowName = "Open FIle", string initialDir = "",
        string[]? fileTypes = null);

    Task<string> SaveFileAsync(
        IntPtr ownerHandle,
        string defaultFileName = "",
        string windowName = "Save File",
        string initialDir = "",
        string[]? fileTypes = null);
}