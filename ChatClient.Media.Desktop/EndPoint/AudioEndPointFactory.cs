using System.Runtime.InteropServices;
using ChatClient.Media.Desktop.EndPoint.Windows;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.Media.EndPoint;
using SIPSorcery.Media;
using SIPSorceryMedia.Abstractions;

namespace ChatClient.Media.Desktop.EndPoint;

public class AudioEndPointFactory : IFactory<IAudioEndPoint>
{
    /// <summary>
    /// 创建对应操作系统平台的音频端点实例。
    /// </summary>
    /// <param name="audioEncoder"></param>
    /// <returns></returns>
    /// <exception cref="PlatformNotSupportedException"></exception>
    public IAudioEndPoint Create()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return new WindowsAudioEndPoint(new AudioEncoder());
        }
        catch (Exception e)
        {
            throw new PlatformNotSupportedException("当前设备不支持的音频通话", e);
        }

        throw new PlatformNotSupportedException("当前平台不支持的音频通话");
    }
}