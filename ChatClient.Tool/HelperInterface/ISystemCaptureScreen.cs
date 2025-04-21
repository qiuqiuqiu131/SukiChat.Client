using System.Drawing;
using System.Drawing.Imaging;

namespace ChatClient.Tool.HelperInterface;

public interface ISystemCaptureScreen
{
    Image CaptureScreen();

    Image CaptureWindow(IntPtr handle);

    void CaptureWindowToFile(IntPtr handle, string filename, ImageFormat format);

    void CaptureScreenToFile(string filename, ImageFormat format);
}