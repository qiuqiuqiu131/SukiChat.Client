namespace ChatClient.Tool.Data;

public class Connect:BindableBase
{
    private bool _isConnectted = false;
    public bool IsConnected
    {
        get => _isConnectted;
        set
        {
            if(SetProperty(ref _isConnectted, value))
                ConnecttedChanged?.Invoke(value);
        }
    }

    public event Action<bool> ConnecttedChanged;
}