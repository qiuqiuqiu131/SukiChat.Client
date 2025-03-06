namespace ChatClient.Tool.Data;

public class LoginData : BindableBase
{
    public event Action LoginDataChanged;
    
    private string? id = null;
    public string? Id
    {
        get => id;
        set
        {
            if(SetProperty(ref id, value))
                LoginDataChanged?.Invoke();
        }
    }
    
    private bool rememberPassword = false;

    public bool RememberPassword
    {
        get => rememberPassword;
        set
        {
            if(SetProperty(ref rememberPassword, value))
                LoginDataChanged?.Invoke();
        }
    }
}