using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Media.Imaging;
using ChatClient.Avalonia;
using ChatClient.BaseService.Services;
using ChatClient.Desktop.Views.UserControls;
using ChatClient.Tool.Data;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.Tools;
using Prism.Commands;
using Prism.Mvvm;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.UserControls;

public class UserHeadEditViewModel : BindableBase
{
    #region Properties

    private Bitmap _currentHead;

    public Bitmap CurrentHead
    {
        get => _currentHead;
        set => SetProperty(ref _currentHead, value);
    }

    private Bitmap _selectedItem;

    public Bitmap SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    private bool isBusy = false;

    public bool IsBusy
    {
        get => isBusy;
        set => SetProperty(ref isBusy, value);
    }

    private ThemeStyle _currentThemeStyle;

    public ThemeStyle CurrentThemeStyle
    {
        get => _currentThemeStyle;
        set => SetProperty(ref _currentThemeStyle, value);
    }

    private ObservableCollection<Bitmap> _headList;

    public ObservableCollection<Bitmap> HeadList
    {
        get => _headList;
        set => SetProperty(ref _headList, value);
    }

    private ISukiDialogManager _dialogManager;

    public ISukiDialogManager DialogManager
    {
        get => _dialogManager;
        set => SetProperty(ref _dialogManager, value);
    }

    public DelegateCommand AddHeadCommnad { get; init; }
    public DelegateCommand SaveHeadCommand { get; init; }
    public DelegateCommand SelectedHeadChangedCommand { get; init; }
    public DelegateCommand CancleCommand { get; init; }

    #endregion

    private Dictionary<int, Bitmap> _headImages;

    public event Action<Bitmap> ImageChanged;
    public Control? View;

    private readonly IUserManager _userManager;

    public UserHeadEditViewModel(IUserManager userManager, IUserService userService, IThemeStyle themeStyle)
    {
        _userManager = userManager;
        _currentThemeStyle = themeStyle.CurrentThemeStyle;
        CurrentHead = userManager.User!.HeadImage;

        AddHeadCommnad = new DelegateCommand(AddHead);
        SaveHeadCommand = new DelegateCommand(SaveHead);
        SelectedHeadChangedCommand = new DelegateCommand(SelectedHeadChanged);
        CancleCommand = new DelegateCommand(() => ((UserHeadEditView)View).Close());

        DialogManager = new SukiDialogManager();

        if (_userManager.User?.HeadCount != 0)
        {
            userService.GetHeadImages(userManager.User).ContinueWith(list =>
            {
                _headImages = list.Result;
                var bitmaps = list.Result.Values.ToList();
                bitmaps.Reverse();
                HeadList = new ObservableCollection<Bitmap>(bitmaps);

                SelectedItem = _headImages[userManager.User.HeadIndex];
            });
        }
        else
        {
            HeadList = new ObservableCollection<Bitmap>();
            _headImages = new Dictionary<int, Bitmap>();
        }
    }

    private void SelectedHeadChanged()
    {
        ImageChanged?.Invoke(SelectedItem);
    }

    private async void AddHead()
    {
        if (View == null) return;

        string filePath = "";
        var window = TopLevel.GetTopLevel(View);
        var handle = window.TryGetPlatformHandle()?.Handle;

        if (handle == null) return;

        filePath = await SystemFileDialog.OpenFileAsync(handle.Value, "选择图片",
            "Image Files\0*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.tiff;*.webp\0");

        if (string.IsNullOrWhiteSpace(filePath)) return;

        // 选择好图像后，直接显示在页面上，但是暂时不许要上传。等用户设置好头像位置，点击确认后，在保存图片并上传。
        byte[] bytes = await System.IO.File.ReadAllBytesAsync(filePath);
        Bitmap? bitmap = null;
        using (var stream = new MemoryStream(bytes))
            bitmap = new Bitmap(stream);

        if (bitmap == null) return;

        // 读取选中图片，显示出来
        ImageChanged?.Invoke(bitmap);
    }

    private async void SaveHead()
    {
        IsBusy = true;
        UserHeadEditView view = View as UserHeadEditView;
        if (view == null) return;

        var imageResize = view.GetImageResize();
        if (imageResize.Scale == 1 && imageResize.MoveX == 0 && imageResize.MoveY == 0)
        {
            IsBusy = false;
            _userManager.User.HeadImage = SelectedItem;
            _userManager.User.HeadIndex = _headImages.FirstOrDefault(x => x.Value == SelectedItem).Key;
            _userManager.SaveUser();

            ((UserHeadEditView)View).Close();
            return;
        }

        Bitmap bitmap = ImageTool.GetHeadImage(imageResize);

        var result = await _userManager.ResetHead(bitmap);
        IsBusy = false;
        if (result)
        {
            HeadList.Insert(0, bitmap);
            _headImages.Add(_userManager.User.HeadIndex, bitmap);
            SelectedItem = bitmap;
            ImageChanged?.Invoke(bitmap);

            ((UserHeadEditView)View).Close();
        }
        else
            _dialogManager.CreateDialog()
                .OfType(NotificationType.Error)
                .WithTitle("上传失败")
                .WithContent("上传头像失败，请检查网络连接并重试")
                .Dismiss().ByClickingBackground()
                .TryShow();
    }
}