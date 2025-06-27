namespace ChatClient.Tool.HelperInterface;

public interface IFactory<out T>
{
    T Create();
}