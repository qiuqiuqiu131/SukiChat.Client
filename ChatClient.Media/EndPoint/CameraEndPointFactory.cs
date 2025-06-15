using System.Runtime.InteropServices;
using ChatClient.Media.EndPoint.Windows;
using ChatClient.Tool.Media.Video;
using SIPSorceryMedia.Encoders;

namespace ChatClient.Media.EndPoint;

public static class CameraEndPointFactory
{
    public static ICameraEndPoint CreateCameraEndPoint()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new WindowsCameraEndPoint(new VpxVideoEncoder(),
                WindowsCameraEndPoint.GetVideoCaptureDevices()[0].ID);
        }
        else
        {
            throw new PlatformNotSupportedException("当前平台不支持的摄像头端点");
        }
    }

    public static List<VideoCaptureDeviceInfo> GetVideoCaptureDevices()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return WindowsCameraEndPoint.GetVideoCaptureDevices();
        }
        else
        {
            throw new PlatformNotSupportedException("当前平台不支持的摄像头端点");
        }
    }
}