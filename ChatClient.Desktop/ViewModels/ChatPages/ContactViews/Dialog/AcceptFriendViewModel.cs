using System;
using System.Collections.Generic;
using System.Linq;
using ChatClient.Tool.Data;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Ioc;
using Prism.Mvvm;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.ChatPages.ContactViews.Dialog;

public class AcceptFriendViewModel : BindableBase
{
    private readonly ISukiDialog _sukidialog;
    private readonly Action<IDialogResult> _requestClose;

    private UserDto _userDto;

    public UserDto UserDto
    {
        get => _userDto;
        set => SetProperty(ref _userDto, value);
    }

    private string? _remark;

    public string? Remark
    {
        get => _remark;
        set => SetProperty(ref _remark, value);
    }

    private string? _group;

    public string? Group
    {
        get => _group;
        set => SetProperty(ref _group, value);
    }

    private List<string> _groups;

    public List<string> Groups
    {
        get => _groups;
        set => SetProperty(ref _groups, value);
    }

    public DelegateCommand CancelCommand { get; init; }
    public DelegateCommand OkCommand { get; init; }

    public AcceptFriendViewModel(ISukiDialog sukidialog, Action<IDialogResult> requestClose, UserDto userDto)
    {
        _sukidialog = sukidialog;
        _requestClose = requestClose;

        UserDto = userDto;

        var userManager = App.Current.Container.Resolve<IUserManager>();
        Groups = userManager.GroupFriends!.Select(d => d.GroupName).Order().ToList();
        Group = "默认分组";

        OkCommand = new DelegateCommand(Ok);
        CancelCommand = new DelegateCommand(() =>
        {
            requestClose(new DialogResult(ButtonResult.Cancel));
            _sukidialog.Dismiss();
        });
    }

    private void Ok()
    {
        var result = new DialogResult(ButtonResult.OK);
        result.Parameters.Add("Remark", Remark);
        result.Parameters.Add("Group", Group);
        _requestClose?.Invoke(result);

        _sukidialog.Dismiss();
    }
}