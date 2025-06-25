using System.Runtime.InteropServices;
using ChatClient.Media.Desktop.EndPoint.Windows;
using SIPSorceryMedia.Abstractions;

namespace ChatClient.Media.Desktop.EndPoint;

public static class AudioEndPointFactory
{
    /// <summary>
    /// 创建对应操作系统平台的音频端点实例。
    /// </summary>
    /// <param name="audioEncoder"></param>
    /// <returns></returns>
    /// <exception cref="PlatformNotSupportedException"></exception>
    public static IAudioEndPoint CreateAudioEndPoint(IAudioEncoder audioEncoder)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return new WindowsAudioEndPoint(audioEncoder);
        }
        catch (Exception e)
        {
            throw new PlatformNotSupportedException("当前设备不支持的音频通话", e);
        }

        throw new PlatformNotSupportedException("当前平台不支持的音频通话");
    }
}