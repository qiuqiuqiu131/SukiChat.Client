using Avalonia.Controls;

namespace ChatClient.Tool.HelperInterface;

public interface ITaskbarFlashHelper
{
    void FlashWindow(Window window, uint flashCount = 8);
    void StopFlashing(Window window);
}