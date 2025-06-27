using System.Runtime.InteropServices;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.Media.Audio;
using NAudio.Wave;

namespace ChatClient.Media.Desktop.AudioRecorder;

public class AudioRecorderFactory : IFactory<IPlatformAudioRecorder>
{
    // 获取可用的录音设备列表
    public List<WaveInCapabilities> GetAvailableDevices()
    {
        var devices = new List<WaveInCapabilities>();
        for (int i = 0; i < WaveInEvent.DeviceCount; i++)
        {
            devices.Add(WaveInEvent.GetCapabilities(i));
        }

        return devices;
    }

    /// <summary>
    /// 创建对应操作系统平台的音频录音器实例。
    /// </summary>
    /// <returns></returns>
    /// <exception cref="PlatformNotSupportedException"></exception>
    public IPlatformAudioRecorder Create()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new WindowsAudioRecorder();
        else
            throw new PlatformNotSupportedException("当前平台不支持的音频录音器");
    }
}