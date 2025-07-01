using ChatClient.Tool.Data;
using ChatClient.Tool.ManagerInterface;

namespace ChatClient.Android.Shared.ViewModels;

public class MainChatViewModel : BindableBase
{
    private readonly IUserManager _userManager;

    #region User

    private UserDetailDto _user;

    public UserDetailDto User
    {
        get => _user;
        set => SetProperty(ref _user, value);
    }

    #endregion

    public IRegionManager RegionManager { get; }

    public MainChatViewModel(IRegionManager regionManager, IUserManager userManager)
    {
        _userManager = userManager;
        RegionManager = regionManager;

        User = _userManager.User!;
    }
}