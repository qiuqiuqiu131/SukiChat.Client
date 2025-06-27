using System.Drawing;
using System.Drawing.Imaging;

namespace ChatClient.Media.Desktop.ScreenCapture;

public interface ISystemCaptureScreen
{
    Image CaptureScreen();

    Image CaptureWindow(IntPtr handle);

    void CaptureWindowToFile(IntPtr handle, string filename, ImageFormat format);

    void CaptureScreenToFile(string filename, ImageFormat format);
}