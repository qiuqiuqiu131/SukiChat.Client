using Avalonia;
using Avalonia.Media.Imaging;
using System.Diagnostics;

namespace ChatClient.Tool.Tools;

public static class ImageTool
{
    public static Bitmap? GetHeadImage(ImageResize resize, int targetSize = 256)
    {
        if (resize.Bitmap == null) return null;

        int width = resize.Bitmap.PixelSize.Width;
        int height = resize.Bitmap.PixelSize.Height;
        int minSize = Math.Min(width, height);
        int cropSize = (int)(minSize / resize.Scale);

        // 计算裁剪区域的起点，位移为图片的比例
        int x = (int)(resize.MoveX * width);
        int y = (int)(resize.MoveY * height);

        // 确保裁剪区域在图像范围内
        x = Math.Max(0, Math.Min(x, width - cropSize));
        y = Math.Max(0, Math.Min(y, height - cropSize));

        var croppedBitmap = new CroppedBitmap(resize.Bitmap, new PixelRect(x, y, cropSize, cropSize));

        // Render the CroppedBitmap to a new Bitmap and resize it
        var renderTarget = new RenderTargetBitmap(new PixelSize(targetSize, targetSize), new Vector(96, 96));
        using (var ctx = renderTarget.CreateDrawingContext(true))
        {
            ctx.DrawImage(croppedBitmap, new Rect(0, 0, cropSize, cropSize), new Rect(0, 0, targetSize, targetSize));
        }

        return renderTarget;
    }

    public static byte[] BitmapToByteArray(this Bitmap bitmap)
    {
        using (var stream = new MemoryStream())
        {
            bitmap.Save(stream);
            return stream.ToArray();
        }
    }

    public static Stream BitmapToStream(this Bitmap bitmap)
    {
        var stream = new MemoryStream();
        bitmap.Save(stream);
        return stream;
    }

    public static Bitmap Clone(this Bitmap bitmap)
    {
        using (var memoryStream = new MemoryStream())
        {
            // 将位图保存到内存流
            bitmap.Save(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            // 从内存流加载位图
            return new Bitmap(memoryStream);
        }
    }

    public static long GetSize(this Bitmap bitmap)
    {
        using (var memoryStream = new MemoryStream())
        {
            // 将位图保存到内存流
            bitmap.Save(memoryStream);
            return memoryStream.Length;
        }
    }

    public static void OpenImageInSystemViewer(Bitmap bitmap)
    {
        // 将 Bitmap 保存到临时文件
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");
        using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
        {
            bitmap.Save(fileStream);
        }

        // 使用系统命令打开图片
        var process = new Process
        {
            StartInfo = new ProcessStartInfo(tempFilePath)
            {
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal
            }
        };
        process.Start();
    }

    public static Bitmap GetLowResolutionBitmap(this Bitmap bitmap, double scale)
    {
        int targetWidth = (int)(bitmap.PixelSize.Width * scale);
        int targetHeight = (int)(bitmap.PixelSize.Height * scale);

        var renderTarget = new RenderTargetBitmap(new PixelSize(targetWidth, targetHeight), new Vector(96, 96));
        using (var ctx = renderTarget.CreateDrawingContext(true))
        {
            ctx.DrawImage(bitmap, new Rect(0, 0, bitmap.PixelSize.Width, bitmap.PixelSize.Height),
                new Rect(0, 0, targetWidth, targetHeight));
        }

        return renderTarget;
    }
}

public class ImageResize
{
    public Bitmap? Bitmap { get; set; }
    public double Scale { get; set; }
    public double MoveX { get; set; }
    public double MoveY { get; set; }
}