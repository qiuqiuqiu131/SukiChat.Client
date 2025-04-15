namespace ChatClient.Tool.ManagerInterface;

public interface IStunServerManager
{
    Task<List<string>> GetStunServersUrl();
}