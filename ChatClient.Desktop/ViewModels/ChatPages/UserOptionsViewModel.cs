using System;
using ChatClient.Desktop.Views.UserControls;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.UserOption;
using ChatClient.Tool.ManagerInterface;
using Material.Icons;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Ioc;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.ChatPages;

public class UserOptionsViewModel : ChatPageBase
{
    private readonly IDialogService _dialogService;
    private UserDto user;

    public UserDto User
    {
        get => user;
        set => SetProperty(ref user, value);
    }

    private UserBasicDetail basicDetail = new UserBasicDetail();

    public UserBasicDetail BasicDetail
    {
        get => basicDetail;
        set => SetProperty(ref basicDetail, value);
    }

    public DelegateCommand EditHeadCommand { get; init; }

    public UserOptionsViewModel(IDialogService dialogService,
        IUserManager userManager)
        : base("用户", MaterialIconKind.User, 2)
    {
        _dialogService = dialogService;

        User = userManager.User!;
        EditHeadCommand = new DelegateCommand(EditHead);

        InitDetail();
    }

    private void InitDetail()
    {
        BasicDetail.Name = User.Name;
        BasicDetail.Introduction = User.Introduction;
        BasicDetail.RegisterDate = DateOnly.FromDateTime(User.RegisteTime);
    }

    private void EditHead()
    {
        _dialogService.Show(nameof(UserHeadEditView));
    }
}