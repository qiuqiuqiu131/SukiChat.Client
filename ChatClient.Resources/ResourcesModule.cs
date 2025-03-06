namespace ChatClient.Resources;

public class ResourcesModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<IResourcesClientPool, ResourcesClientPool>();
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
    }
}