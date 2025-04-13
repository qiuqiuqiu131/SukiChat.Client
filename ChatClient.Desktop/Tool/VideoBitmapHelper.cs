using System;
using Avalonia.Media.Imaging;
using ChatClient.Media;
using SIPSorceryMedia.Abstractions;

namespace ChatClient.Desktop.Tool;

public static class VideoBitmapHelper
{
    public static void OperateVideoFrame(VideoFrameReceivedEventArgs args, WriteableBitmap _VideoFrame)
    {
        // 是否需要水平镜像 - 只为摄像头源镜像
        bool shouldMirror = args.IsCamera;

        // 将帧数据写入位图
        using var frameBitmap = _VideoFrame.Lock();

        int bytesPerPixel = args.PixelFormat == VideoPixelFormatsEnum.Rgb ||
                            args.PixelFormat == VideoPixelFormatsEnum.Bgr
            ? 3
            : 4;

        // 如果格式相符，直接复制
        if ((args.PixelFormat == VideoPixelFormatsEnum.Bgra && bytesPerPixel == 4) ||
            (args.PixelFormat == VideoPixelFormatsEnum.Bgr && bytesPerPixel == 3))
        {
            unsafe
            {
                byte* destPtr = (byte*)frameBitmap.Address;
                fixed (byte* srcPtr = args.FrameData)
                {
                    if (bytesPerPixel == 4)
                    {
                        // BGRA 数据处理
                        for (int y = 0; y < args.Height; y++)
                        {
                            for (int x = 0; x < args.Width; x++)
                            {
                                // 根据来源决定是否镜像
                                int srcX = shouldMirror ? (args.Width - 1 - x) : x;
                                int srcIndex = (y * args.Width + srcX) * 4;
                                int destIndex = y * frameBitmap.RowBytes + x * 4;

                                destPtr[destIndex] = srcPtr[srcIndex]; // B
                                destPtr[destIndex + 1] = srcPtr[srcIndex + 1]; // G
                                destPtr[destIndex + 2] = srcPtr[srcIndex + 2]; // R
                                destPtr[destIndex + 3] = srcPtr[srcIndex + 3]; // A
                            }
                        }
                    }
                    else
                    {
                        // BGR 转 BGRA (添加Alpha通道)
                        for (int y = 0; y < args.Height; y++)
                        {
                            for (int x = 0; x < args.Width; x++)
                            {
                                // 根据来源决定是否镜像
                                int srcX = shouldMirror ? (args.Width - 1 - x) : x;
                                int srcIndex = (y * args.Width + srcX) * 3;
                                int destIndex = y * frameBitmap.RowBytes + x * 4;

                                destPtr[destIndex] = srcPtr[srcIndex]; // B
                                destPtr[destIndex + 1] = srcPtr[srcIndex + 1]; // G
                                destPtr[destIndex + 2] = srcPtr[srcIndex + 2]; // R
                                destPtr[destIndex + 3] = 255; // A
                            }
                        }
                    }
                }
            }
        }
        else if (args.PixelFormat == VideoPixelFormatsEnum.Rgb)
        {
            unsafe
            {
                byte* destPtr = (byte*)frameBitmap.Address;
                fixed (byte* srcPtr = args.FrameData)
                {
                    // RGB 转 BGRA
                    for (int y = 0; y < args.Height; y++)
                    {
                        for (int x = 0; x < args.Width; x++)
                        {
                            // 根据来源决定是否镜像
                            int srcX = shouldMirror ? (args.Width - 1 - x) : x;
                            int srcIndex = (y * args.Width + srcX) * 3;
                            int destIndex = y * frameBitmap.RowBytes + x * 4;

                            destPtr[destIndex] = srcPtr[srcIndex + 2]; // B (从R位置)
                            destPtr[destIndex + 1] = srcPtr[srcIndex + 1]; // G
                            destPtr[destIndex + 2] = srcPtr[srcIndex]; // R (从B位置)
                            destPtr[destIndex + 3] = 255; // A
                        }
                    }
                }
            }
        }
        else
        {
            Console.WriteLine($"不支持的像素格式: {args.PixelFormat}");
        }
    }
}