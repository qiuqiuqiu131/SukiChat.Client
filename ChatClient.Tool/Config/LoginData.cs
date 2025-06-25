namespace ChatClient.Tool.Config;

public class LoginData : BindableBase
{
    public event Action LoginDataChanged;

    private string? id = null;

    public string? Id
    {
        get => id;
        set
        {
            if (SetProperty(ref id, value))
                LoginDataChanged?.Invoke();
        }
    }

    private bool rememberPassword = true;

    public bool RememberPassword
    {
        get => rememberPassword;
        set
        {
            if (SetProperty(ref rememberPassword, value))
                LoginDataChanged?.Invoke();
        }
    }
}