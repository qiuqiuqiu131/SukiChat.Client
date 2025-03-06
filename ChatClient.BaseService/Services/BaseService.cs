namespace ChatClient.BaseService.Services;

public abstract class BaseService
{
    protected readonly IScopedProvider _scopedProvider;

    public BaseService(IContainerProvider containerProvider)
    {
        _scopedProvider = containerProvider.CreateScope();
    }
}