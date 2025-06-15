using System.Runtime.InteropServices;
using ChatClient.Media.EndPoint.Windows;
using SIPSorceryMedia.Abstractions;

namespace ChatClient.Media.EndPoint;

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
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new WindowsAudioEndPoint(audioEncoder);
        else
            throw new PlatformNotSupportedException("当前平台不支持的音频端点");
    }
}