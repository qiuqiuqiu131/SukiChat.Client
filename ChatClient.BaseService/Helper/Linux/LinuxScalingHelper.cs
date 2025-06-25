using System.Diagnostics;
using System.Runtime.InteropServices;
using ChatClient.Tool.HelperInterface;

namespace ChatClient.BaseService.Helper.Linux;

internal class LinuxScalingHelper : ISystemScalingHelper
{
    // X11库导入
    [DllImport("libX11.so.6")]
    private static extern IntPtr XOpenDisplay(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern int XCloseDisplay(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern int XDisplayWidth(IntPtr display, int screen);

    [DllImport("libX11.so.6")]
    private static extern int XDisplayWidthMM(IntPtr display, int screen);

    [DllImport("libX11.so.6")]
    private static extern int XDefaultScreen(IntPtr display);

    public double GetScalingFactor()
    {
        try
        {
            // 尝试从环境变量获取缩放因子
            double envScaling = GetScalingFromEnvironment();
            if (envScaling > 0)
            {
                return envScaling;
            }

            // 尝试使用X11库获取缩放因子
            double x11Scaling = GetScalingFromX11();
            if (x11Scaling > 0)
            {
                return x11Scaling;
            }

            // 尝试使用xdpyinfo命令获取
            double xdpyinfoScaling = GetScalingFromXdpyinfo();
            if (xdpyinfoScaling > 0)
            {
                return xdpyinfoScaling;
            }

            // 所有方法都失败，返回默认值
            return 1.0;
        }
        catch
        {
            // 出现任何异常都返回默认缩放比例
            return 1.0;
        }
    }

    private double GetScalingFromEnvironment()
    {
        // 检查常用的Linux环境变量
        string? gdkScale = Environment.GetEnvironmentVariable("GDK_SCALE");
        if (!string.IsNullOrEmpty(gdkScale) && double.TryParse(gdkScale, out double scale) && scale > 0)
        {
            return scale;
        }

        string? gdkDpiScale = Environment.GetEnvironmentVariable("GDK_DPI_SCALE");
        if (!string.IsNullOrEmpty(gdkDpiScale) && double.TryParse(gdkDpiScale, out double dpiScale) && dpiScale > 0)
        {
            return dpiScale;
        }

        string? qtScaleFactor = Environment.GetEnvironmentVariable("QT_SCALE_FACTOR");
        if (!string.IsNullOrEmpty(qtScaleFactor) && double.TryParse(qtScaleFactor, out double qtScale) && qtScale > 0)
        {
            return qtScale;
        }

        return 0;
    }

    private double GetScalingFromX11()
    {
        IntPtr display = XOpenDisplay(IntPtr.Zero);
        if (display == IntPtr.Zero)
        {
            return 0;
        }

        try
        {
            int screen = XDefaultScreen(display);
            int widthPixels = XDisplayWidth(display, screen);
            int widthMM = XDisplayWidthMM(display, screen);

            if (widthMM <= 0)
                return 0;

            // 计算DPI： (pixels / mm) * 25.4(mm/inch)
            double dpi = (widthPixels / (double)widthMM) * 25.4;

            // 标准DPI是96，缩放比例 = DPI / 96
            return dpi / 96.0;
        }
        finally
        {
            XCloseDisplay(display);
        }
    }

    private double GetScalingFromXdpyinfo()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "xdpyinfo",
                Arguments = "-ext RANDR",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
                return 0;

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // 解析输出内容，寻找分辨率信息
            string[] lines = output.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("resolution:") && line.Contains("x"))
                {
                    var parts = line.Split(new[] { "resolution:" }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1)
                    {
                        var dpiParts = parts[1].Trim().Split('x');
                        if (dpiParts.Length >= 1 && double.TryParse(dpiParts[0].Trim(), out double dpiX))
                        {
                            return dpiX / 96.0;
                        }
                    }
                }
            }

            return 0;
        }
        catch
        {
            return 0;
        }
    }
}