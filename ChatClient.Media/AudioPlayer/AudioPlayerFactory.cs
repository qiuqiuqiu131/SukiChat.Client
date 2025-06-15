using System.Runtime.InteropServices;
using ChatClient.Tool.Media.Audio;

namespace ChatClient.Media.AudioPlayer;

public static class AudioPlayerFactory
{
    /// <summary>
    /// 创建对应操作系统平台的音频播放器实例。
    /// </summary>
    /// <returns></returns>
    /// <exception cref="PlatformNotSupportedException"></exception>
    public static IPlatformAudioPlayer CreateAudioPlayer()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new WindowsAudioPlayer();
        else
            throw new PlatformNotSupportedException("当前平台不支持的音频播放器");
    }
}