using ChatClient.Desktop.UIEntity;

namespace ChatClient.Tool.Data.UserOption;

public class UserBasicDetail : BindableBase
{
    private string? _id;

    public string? Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    private string? _name;

    public string? Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    private string? _introduction;

    public string? Introduction
    {
        get => _introduction;
        set => SetProperty(ref _introduction, value);
    }

    private Sex _sex;

    public Sex Sex
    {
        get => _sex;
        set => SetProperty(ref _sex, value);
    }

    private DateOnly _birthday;

    public DateOnly Birthday
    {
        get => _birthday;
        set
        {
            SetProperty(ref _birthday, value);
            RaisePropertyChanged(nameof(Age));
        }
    }

    public int Age => DateTime.Now.Year - Birthday.Year;

    private DateOnly _registerDate;

    public DateOnly RegisterDate
    {
        get => _registerDate;
        set => SetProperty(ref _registerDate, value);
    }
}