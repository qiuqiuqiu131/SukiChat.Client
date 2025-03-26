using System;
using System.Collections.Generic;
using Avalonia.Controls.Notifications;
using ChatClient.Desktop.UIEntity;
using ChatClient.Desktop.Views.UserControls;
using ChatClient.Tool.Data;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.UserControls;

public class EditUserDataViewModel : BindableBase
{
    private readonly ISukiDialog _dialog;
    private readonly IEventAggregator _eventAggregator;
    private readonly IUserManager _userManager;
    private readonly IDialogService _dialogService;

    #region Property

    private UserDto _userDto;

    public UserDto UserDto
    {
        get => _userDto;
        set => SetProperty(ref _userDto, value);
    }

    private string name;

    public string Name
    {
        get => name;
        set => SetProperty(ref name, value);
    }

    private string introduction;

    public string Introduction
    {
        get => introduction;
        set => SetProperty(ref introduction, value);
    }

    private Sex sex;

    public Sex Sex
    {
        get => sex;
        set => SetProperty(ref sex, value);
    }

    private DateOnly? _dateTime;

    public DateOnly? DateTime
    {
        get => _dateTime;
        set => SetProperty(ref _dateTime, value);
    }

    #endregion

    public IEnumerable<Sex> SexEnum { get; set; } = Enum.GetValues<Sex>();

    public DelegateCommand EditHeadCommand { get; init; }
    public DelegateCommand CancelCommand { get; init; }
    public DelegateCommand SaveCommand { get; init; }

    public EditUserDataViewModel(ISukiDialog dialog, UserDto userDto)
    {
        _dialog = dialog;
        _eventAggregator = App.Current.Container.Resolve<IEventAggregator>();
        _userManager = App.Current.Container.Resolve<IUserManager>();
        _dialogService = App.Current.Container.Resolve<IDialogService>();


        UserDto = userDto;
        Name = UserDto.Name;
        Introduction = UserDto.Introduction ?? "";
        Sex = UserDto.Sex;
        DateTime = UserDto.Birth;

        EditHeadCommand = new DelegateCommand(EditHead);
        CancelCommand = new DelegateCommand(() => dialog!.Dismiss());
        SaveCommand = new DelegateCommand(Save);
    }

    private void Save()
    {
        UserDto.Name = Name;
        UserDto.Sex = Sex;
        UserDto.Birth = DateTime;
        UserDto.Introduction = Introduction;
        _userManager.SaveUser();

        _dialog.Dismiss();

        _eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
        {
            Message = "用户信息更改成功",
            Type = NotificationType.Success
        });
    }

    private void EditHead()
    {
        _dialogService.Show(nameof(UserHeadEditView));
    }
}