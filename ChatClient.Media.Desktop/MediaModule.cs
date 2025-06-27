using System.Runtime.InteropServices;
using ChatClient.Media.Desktop.AudioPlayer;
using ChatClient.Media.Desktop.AudioRecorder;
using ChatClient.Media.Desktop.EndPoint;
using ChatClient.Media.Desktop.ScreenCapture;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.Media.Audio;
using ChatClient.Tool.Media.Call;
using ChatClient.Tool.Media.EndPoint;

namespace ChatClient.Media.Desktop;

public class MediaModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.Register<IFactory<IPlatformAudioRecorder>, AudioRecorderFactory>()
            .Register<IFactory<IPlatformAudioPlayer>, AudioPlayerFactory>()
            .Register<IFactory<IAudioEndPoint>, AudioEndPointFactory>()
            .Register<IFactory<ICameraEndPoint>, CameraEndPointFactory>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            containerRegistry.Register<ISystemCaptureScreen, WindowsCaptureScreenHelper>();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            containerRegistry.Register<ISystemCaptureScreen, LinuxCaptureScreenHelper>();
        }
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
    }
}