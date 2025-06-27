using System.Runtime.InteropServices;
using ChatClient.Media.Desktop.EndPoint.Windows;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.Media.EndPoint;
using ChatClient.Tool.Media.Video;
using SIPSorceryMedia.Encoders;

namespace ChatClient.Media.Desktop.EndPoint;

public class CameraEndPointFactory : IFactory<ICameraEndPoint>
{
    public ICameraEndPoint Create()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new WindowsCameraEndPoint(new VpxVideoEncoder(),
                    WindowsCameraEndPoint.GetVideoCaptureDevices()[0].ID);
            }
        }
        catch (Exception e)
        {
            throw new PlatformNotSupportedException("当前设备不支持的摄像头通话", e);
        }

        throw new PlatformNotSupportedException("当前平台不支持的摄像头通话");
    }

    public static List<VideoCaptureDeviceInfo> GetVideoCaptureDevices()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return WindowsCameraEndPoint.GetVideoCaptureDevices();
        }
        else
        {
            throw new PlatformNotSupportedException("当前平台不支持的摄像头通话");
        }
    }
}