using System.Runtime.InteropServices;
using ChatClient.Tool.HelperInterface;

namespace ChatClient.BaseService.Helper;

public class WindowScalingHelper : ISystemScalingHelper
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hwnd);

    [DllImport("gdi32.dll")]
    private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    private const int LOGPIXELSX = 88; // 获取水平 DPI
    private const int LOGPIXELSY = 90; // 获取垂直 DPI

    public double GetScalingFactor()
    {
        IntPtr hdc = GetDC(IntPtr.Zero);
        if (hdc == IntPtr.Zero)
            return 1.0;

        int dpiX = GetDeviceCaps(hdc, LOGPIXELSX);
        ReleaseDC(IntPtr.Zero, hdc);

        // 标准 DPI 是 96，缩放比例 = DPI / 96
        return dpiX / 96.0;
    }
}