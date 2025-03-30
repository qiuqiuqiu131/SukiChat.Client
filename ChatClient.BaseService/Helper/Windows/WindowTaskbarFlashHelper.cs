using System.Runtime.InteropServices;
using Avalonia.Controls;
using ChatClient.Tool.HelperInterface;

namespace ChatClient.BaseService.Helper;

internal class WindowTaskbarFlashHelper : ITaskbarFlashHelper
{
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

    [StructLayout(LayoutKind.Sequential)]
    private struct FLASHWINFO
    {
        public uint cbSize;
        public IntPtr hwnd;
        public uint dwFlags;
        public uint uCount;
        public uint dwTimeout;
    }

    // 闪烁停止
    public const uint FLASHW_STOP = 0;

    // 闪烁标题栏
    public const uint FLASHW_CAPTION = 1;

    // 闪烁任务栏按钮
    public const uint FLASHW_TRAY = 2;

    // 同时闪烁标题栏和任务栏按钮
    public const uint FLASHW_ALL = 3;

    // 闪烁直到窗口获得焦点
    public const uint FLASHW_TIMERNOFG = 12;

    public void FlashWindow(Window window, uint flashCount = 8)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var hwnd = window.TryGetPlatformHandle().Handle;
            FLASHWINFO fInfo = new FLASHWINFO
            {
                hwnd = hwnd,
                dwFlags = FLASHW_TRAY | FLASHW_TIMERNOFG,
                uCount = flashCount,
                cbSize = (uint)Marshal.SizeOf(typeof(FLASHWINFO))
            };
            FlashWindowEx(ref fInfo);
        }
    }

    public void StopFlashing(Window window)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var hwnd = window.TryGetPlatformHandle().Handle;
            FLASHWINFO fInfo = new FLASHWINFO
            {
                hwnd = hwnd,
                dwFlags = FLASHW_STOP,
                cbSize = (uint)Marshal.SizeOf(typeof(FLASHWINFO))
            };
            FlashWindowEx(ref fInfo);
        }
    }
}