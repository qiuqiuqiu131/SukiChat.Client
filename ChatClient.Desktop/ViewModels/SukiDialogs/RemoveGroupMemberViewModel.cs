using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Notifications;
using ChatClient.BaseService.Services.Interface;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.SukiDialogs;

public class RemoveGroupMemberViewModel : BindableBase
{
    private readonly ISukiDialog _sukiDialog;
    private readonly IDialogParameters _dialogParameters;
    private readonly Action<IDialogResult>? _requestClose;
    private readonly IContainerProvider _containerProvider;

    private GroupRelationDto? groupRelationDto;

    public List<GroupMemberDto> GroupMembers =>
        groupRelationDto?.GroupDto?.GroupMembers.Where(d => d.Status == 2).ToList() ?? [];

    private AvaloniaList<GroupMemberDto> selectedMembers = new();

    public AvaloniaList<GroupMemberDto> SelectedMembers
    {
        get => selectedMembers;
        set => SetProperty(ref selectedMembers, value);
    }

    public DelegateCommand CancelCommand { get; }
    public DelegateCommand ConfirmCommand { get; }

    public RemoveGroupMemberViewModel(ISukiDialog sukiDialog, IDialogParameters dialogParameters,
        Action<IDialogResult>? requestClose)
    {
        _sukiDialog = sukiDialog;
        _dialogParameters = dialogParameters;
        _requestClose = requestClose;
        _containerProvider = App.Current.Container;

        groupRelationDto = _dialogParameters.GetValue<GroupRelationDto>("GroupRelationDto");

        ConfirmCommand = new DelegateCommand(Confirm, CanConfirm);
        CancelCommand = new DelegateCommand(() =>
        {
            requestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
            sukiDialog.Dismiss();
        });

        SelectedMembers.CollectionChanged += (_, _) => { ConfirmCommand.RaiseCanExecuteChanged(); };
    }

    private bool CanConfirm() => SelectedMembers.Count > 0;

    /// <summary>
    /// 移除群成员
    /// </summary>
    private async void Confirm()
    {
        var groupService = _containerProvider.Resolve<IGroupService>();
        var userManager = _containerProvider.Resolve<IUserManager>();
        foreach (var selectedMember in SelectedMembers)
            await groupService.RemoveMemberRequest(userManager.User.Id, groupRelationDto.Id, selectedMember.UserId);

        var eventAggregator = _containerProvider.Resolve<IEventAggregator>();
        eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
        {
            Message = "群成员移除成功",
            Type = NotificationType.Success
        });

        _requestClose?.Invoke(new DialogResult(ButtonResult.OK));
        _sukiDialog.Dismiss();
    }
}