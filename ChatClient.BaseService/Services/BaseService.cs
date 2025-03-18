namespace ChatClient.BaseService.Services;

public abstract class BaseService : IDisposable
{
    protected readonly IScopedProvider _scopedProvider;

    public BaseService(IContainerProvider containerProvider)
    {
        _scopedProvider = containerProvider.CreateScope();
        // Console.WriteLine($"create {this.GetType().Name}");
    }

    public void Dispose()
    {
        //Console.WriteLine($"dispose {this.GetType().Name}");
        _scopedProvider.Dispose();
    }
}