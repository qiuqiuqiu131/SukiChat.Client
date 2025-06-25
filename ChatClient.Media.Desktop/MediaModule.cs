using ChatClient.Media.Desktop.CallOperator;
using ChatClient.Tool.Media.Call;

namespace ChatClient.Media.Desktop;

public class MediaModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.Register<TelephoneCallOperator>();
        containerRegistry.Register<VideoCallOperator>();

        containerRegistry.RegisterSingleton<CallManager.CallManager>();
        containerRegistry.RegisterSingleton<ICallManager>(d => d.Resolve<CallManager.CallManager>());
        containerRegistry.RegisterSingleton<ICallOperator>(d => d.Resolve<CallManager.CallManager>());
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
    }
}