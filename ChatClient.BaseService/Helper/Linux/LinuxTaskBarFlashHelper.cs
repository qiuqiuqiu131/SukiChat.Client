using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using ChatClient.Tool.HelperInterface;

namespace ChatClient.BaseService.Helper.Linux;

/// <summary>
/// Linux系统下的任务栏闪烁实现
/// 使用X11的urgency hint机制
/// </summary>
internal class LinuxTaskbarFlashHelper : ITaskbarFlashHelper
{
    // X11库导入
    [DllImport("libX11.so.6")]
    private static extern IntPtr XOpenDisplay(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern int XFree(IntPtr data);

    [DllImport("libX11.so.6")]
    private static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property,
        IntPtr type, int format, int mode, ref int data, int nelements);

    [DllImport("libX11.so.6")]
    private static extern IntPtr XInternAtom(IntPtr display, string atom_name, bool only_if_exists);

    [DllImport("libX11.so.6")]
    private static extern int XFlush(IntPtr display);

    // XLib常量
    private const int PropModeReplace = 0;
    private const int XA_INTEGER = 19;
    private const int XA_ATOM = 4;
    private const int XA_WINDOW = 33;
    private const int FORMAT_32 = 32;
    private const int XUrgencyHint = (1 << 8);

    // 备用通知机制，使用libnotify发送桌面通知
    private void SendDesktopNotification(string title, string message)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "notify-send",
                Arguments = $"--app-name=\"ChatClient\" \"{title}\" \"{message}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            process?.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"发送桌面通知失败: {ex.Message}");
        }
    }

    // 设置窗口的urgency hint属性
    private bool SetUrgencyHint(IntPtr windowHandle, bool urgent)
    {
        try
        {
            IntPtr display = XOpenDisplay(IntPtr.Zero);
            if (display == IntPtr.Zero)
            {
                Console.WriteLine("无法连接到X服务器");
                return false;
            }

            IntPtr wmHintsAtom = XInternAtom(display, "_MOTIF_WM_HINTS", false);
            if (wmHintsAtom == IntPtr.Zero)
            {
                XFree(display);
                return false;
            }

            // 定义Motif WM Hints结构
            // 参考: https://www.x.org/releases/X11R7.6/doc/xorg-docs/specs/ICCCM/icccm.html
            int[] hints = new int[5];
            hints[0] = 1 << 1; // flags: 设置hints
            hints[1] = urgent ? XUrgencyHint : 0; // decoration: 如果urgent为true则设置urgency hint

            int result = XChangeProperty(
                display,
                windowHandle,
                wmHintsAtom,
                wmHintsAtom,
                FORMAT_32,
                PropModeReplace,
                ref hints[0],
                hints.Length);

            XFlush(display);
            XFree(display);

            return result == 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"设置urgency hint失败: {ex.Message}");
            return false;
        }
    }

    public void FlashWindow(Window window, uint flashCount = 8)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return;

        var hwnd = window.TryGetPlatformHandle().Handle;

        // 设置窗口urgency hint
        if (!SetUrgencyHint(hwnd, true))
        {
            // 如果设置urgency hint失败，尝试使用桌面通知作为备选方案
            SendDesktopNotification("新消息", "您有新的消息，请查看");
        }
    }

    public void StopFlashing(Window window)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return;

        var hwnd = window.TryGetPlatformHandle().Handle;

        // 清除urgency hint
        SetUrgencyHint(hwnd, false);
    }
}