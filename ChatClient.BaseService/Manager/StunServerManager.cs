using ChatClient.Tool.Config;
using ChatClient.Tool.ManagerInterface;

namespace ChatClient.BaseService.Manager;

public class StunServerManager : IStunServerManager
{
    private readonly AppSettings _appSettings;
    private readonly HttpClient httpClient;

    private List<string>? stunServerUrl;

    public StunServerManager(AppSettings appSettings)
    {
        _appSettings = appSettings;
        httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
    }

    public async Task<List<string>> GetStunServersUrl()
    {
        if (stunServerUrl == null || !stunServerUrl.Any())
        {
            await GetStunServersUrlAsync();
        }

        if (stunServerUrl == null || !stunServerUrl.Any())
            stunServerUrl = _appSettings.IceServers.ToList();
        return stunServerUrl?.Take(10).ToList() ?? [];
    }

    public async Task GetStunServersUrlAsync()
    {
        string url = _appSettings.StunServerUrl;
        try
        {
            var response = await httpClient.GetStringAsync(url);
            stunServerUrl = response
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => $"stun:{line.Trim()}")
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取STUN服务器列表失败: {ex.Message}");
            stunServerUrl = new List<string>();
        }
    }
}