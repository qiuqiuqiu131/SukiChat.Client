using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ChatClient.Media.Desktop.ScreenCapture;

public class LinuxCaptureScreenHelper : ISystemCaptureScreen
{
    // X11库导入
    [DllImport("libX11.so.6")]
    private static extern IntPtr XOpenDisplay(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern int XCloseDisplay(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern IntPtr XDefaultRootWindow(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern int XGetWindowAttributes(IntPtr display, IntPtr window, ref XWindowAttributes attributes);

    [DllImport("libX11.so.6")]
    private static extern IntPtr XGetImage(IntPtr display, IntPtr drawable, int x, int y,
        int width, int height, ulong plane_mask, int format);

    [DllImport("libX11.so.6")]
    private static extern void XDestroyImage(IntPtr ximage);

    [DllImport("libX11.so.6")]
    private static extern IntPtr XGetSubImage(IntPtr display, IntPtr drawable, int x, int y,
        uint width, uint height, ulong plane_mask, int format, IntPtr dest_image, int dest_x, int dest_y);

    [StructLayout(LayoutKind.Sequential)]
    private struct XWindowAttributes
    {
        public int x;
        public int y;
        public int width;
        public int height;
        public int border_width;
        public int depth;
        public IntPtr visual;

        public IntPtr root;
        // ... 其他字段省略
    }

    // X11常量
    private const int ZPixmap = 2;
    private const ulong AllPlanes = 0xFFFFFFFF;

    public Image CaptureScreen()
    {
        try
        {
            // 尝试使用本地X11实现
            return CaptureScreenWithX11();
        }
        catch
        {
            // 如果X11库调用失败，回退到使用外部工具
            return CaptureScreenWithExternalTool();
        }
    }

    public Image CaptureWindow(IntPtr handle)
    {
        try
        {
            // 尝试使用本地X11实现
            return CaptureWindowWithX11(handle);
        }
        catch
        {
            // 如果X11库调用失败，回退到使用外部工具
            return CaptureWindowWithExternalTool(handle);
        }
    }

    public void CaptureWindowToFile(IntPtr handle, string filename, ImageFormat format)
    {
        Image img = CaptureWindow(handle);
        img.Save(filename, format);
    }

    public void CaptureScreenToFile(string filename, ImageFormat format)
    {
        Image img = CaptureScreen();
        img.Save(filename, format);
    }

    private Image CaptureScreenWithX11()
    {
        IntPtr display = XOpenDisplay(IntPtr.Zero);
        if (display == IntPtr.Zero)
        {
            throw new InvalidOperationException("无法连接到X服务器");
        }

        try
        {
            IntPtr rootWindow = XDefaultRootWindow(display);

            XWindowAttributes attributes = new XWindowAttributes();
            XGetWindowAttributes(display, rootWindow, ref attributes);

            IntPtr xImage = XGetImage(display, rootWindow, 0, 0, attributes.width, attributes.height,
                AllPlanes, ZPixmap);

            if (xImage == IntPtr.Zero)
            {
                throw new InvalidOperationException("无法获取屏幕图像");
            }

            try
            {
                // 将XImage转换为Bitmap
                return ConvertXImageToBitmap(xImage, attributes.width, attributes.height);
            }
            finally
            {
                XDestroyImage(xImage);
            }
        }
        finally
        {
            XCloseDisplay(display);
        }
    }

    private Image CaptureWindowWithX11(IntPtr window)
    {
        IntPtr display = XOpenDisplay(IntPtr.Zero);
        if (display == IntPtr.Zero)
        {
            throw new InvalidOperationException("无法连接到X服务器");
        }

        try
        {
            XWindowAttributes attributes = new XWindowAttributes();
            XGetWindowAttributes(display, window, ref attributes);

            IntPtr xImage = XGetImage(display, window, 0, 0, attributes.width, attributes.height,
                AllPlanes, ZPixmap);

            if (xImage == IntPtr.Zero)
            {
                throw new InvalidOperationException("无法获取窗口图像");
            }

            try
            {
                // 将XImage转换为Bitmap
                return ConvertXImageToBitmap(xImage, attributes.width, attributes.height);
            }
            finally
            {
                XDestroyImage(xImage);
            }
        }
        finally
        {
            XCloseDisplay(display);
        }
    }

    // 使用外部工具实现的屏幕捕获方法
    private Image CaptureScreenWithExternalTool()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"screenshot_{Guid.NewGuid()}.png");

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "scrot", // 也可以尝试import (ImageMagick)或gnome-screenshot
                Arguments = $"-z \"{tempFile}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        process.Start();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"scrot命令执行失败: {process.StandardError.ReadToEnd()}");
        }

        if (System.IO.File.Exists(tempFile))
        {
            Image image = Image.FromFile(tempFile);
            // 创建一个副本，以便可以删除临时文件
            Image result = new Bitmap(image);
            image.Dispose();
            System.IO.File.Delete(tempFile);
            return result;
        }

        throw new FileNotFoundException("无法创建屏幕截图文件");
    }

    private Image CaptureWindowWithExternalTool(IntPtr handle)
    {
        // Linux中，窗口句柄通常是X11窗口ID
        string windowId = handle.ToString();
        string tempFile = Path.Combine(Path.GetTempPath(), $"window_{Guid.NewGuid()}.png");

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "import", // ImageMagick的import工具
                Arguments = $"-window {windowId} \"{tempFile}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        process.Start();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"import命令执行失败: {process.StandardError.ReadToEnd()}");
        }

        if (System.IO.File.Exists(tempFile))
        {
            Image image = Image.FromFile(tempFile);
            // 创建一个副本，以便可以删除临时文件
            Image result = new Bitmap(image);
            image.Dispose();
            System.IO.File.Delete(tempFile);
            return result;
        }

        throw new FileNotFoundException("无法创建窗口截图文件");
    }

    // 将XImage转换为Bitmap的辅助方法
    private Bitmap ConvertXImageToBitmap(IntPtr xImage, int width, int height)
    {
        // 这里需要根据XImage的具体内存布局来实现
        // 由于XImage结构较为复杂，这里提供一个简化的实现

        // 创建一个空白位图
        Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        // 通过XImage的字段获取像素数据
        // 实际实现中需要根据X11库的内存布局来读取像素数据
        // 并将其复制到bitmap中

        // 注意：真实实现会比这复杂得多，需要根据X11的具体内存布局访问XImage中的像素数据

        return bitmap;

        // 如果直接实现过于复杂，可以考虑使用临时文件方案：
        // 1. 使用XWriteBitmapFile将XImage写入临时文件
        // 2. 使用Image.FromFile加载该文件
        // 3. 创建一个新的Bitmap作为副本
        // 4. 删除临时文件
        // 5. 返回Bitmap副本
    }
}