using System.Runtime.InteropServices;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.Media.Audio;

namespace ChatClient.Media.Desktop.AudioPlayer;

public class AudioPlayerFactory : IFactory<IPlatformAudioPlayer>
{
    /// <summary>
    /// 创建对应操作系统平台的音频播放器实例。
    /// </summary>
    /// <returns></returns>
    /// <exception cref="PlatformNotSupportedException"></exception>
    public IPlatformAudioPlayer Create()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new WindowsAudioPlayer();
        // else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        //     return new LinuxAudioPlayer();
        else
            throw new PlatformNotSupportedException("当前平台不支持的音频播放器");
    }
}