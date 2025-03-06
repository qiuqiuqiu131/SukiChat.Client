using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using ChatClient.Desktop.Views;

namespace ChatClient.Desktop.Tool;

public static class ScreenShotHelper
{
    public static Task<Bitmap> ScreenShot()
    {
        TaskCompletionSource<Bitmap> task = new TaskCompletionSource<Bitmap>();
        var scrrenShotWindow = new ScreenshotWindow(task);
        scrrenShotWindow.Show();

        return task.Task;
    }
}