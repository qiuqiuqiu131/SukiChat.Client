namespace ChatClient.BaseService.Services;

public abstract class BaseService : IDisposable
{
    protected readonly IScopedProvider _scopedProvider;

    public BaseService(IContainerProvider containerProvider)
    {
        _scopedProvider = containerProvider.CreateScope();
    }

    public void Dispose()
    {
        _scopedProvider.Dispose();
    }
}