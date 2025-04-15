using ChatClient.Tool.ManagerInterface;
using Microsoft.Extensions.Configuration;

namespace ChatClient.BaseService.Manager;

public class StunServerManager : IStunServerManager
{
    private readonly IConfigurationRoot _configuration;
    private readonly HttpClient httpClient;

    private List<string>? stunServerUrl;

    public StunServerManager(IConfigurationRoot configuration)
    {
        _configuration = configuration;
        httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
    }

    public async Task<List<string>> GetStunServersUrl()
    {
        if (stunServerUrl == null || !stunServerUrl.Any())
        {
            await GetStunServersUrlAsync();
        }

        if (stunServerUrl == null || !stunServerUrl.Any())
            stunServerUrl = _configuration.GetSection("IceServers")?.Get<List<string>>();
        return stunServerUrl?.Take(10).ToList() ?? [];
    }

    public async Task GetStunServersUrlAsync()
    {
        string url = _configuration["StunServerUrl"] ??
                     "https://raw.githubusercontent.com/pradt2/always-online-stun/master/valid_ipv4s.txt";
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