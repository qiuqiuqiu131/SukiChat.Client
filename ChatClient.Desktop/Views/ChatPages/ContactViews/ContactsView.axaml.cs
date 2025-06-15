using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ChatClient.Desktop.ViewModels.ChatPages.ContactViews;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Friend;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Events;
using Material.Icons;
using Material.Icons.Avalonia;
using Prism.Events;
using Prism.Ioc;
using Prism.Navigation;

namespace ChatClient.Desktop.Views.ChatPages.ContactViews;

public partial class ContactsView : UserControl, IDestructible
{
    // 在类的顶部添加私有字段来存储活动菜单
    private ContextMenu? _activeContextMenu;

    private SubscriptionToken token1;
    private SubscriptionToken token2;
    private SubscriptionToken token3;

    public ContactsView()
    {
        InitializeComponent();

        var eventAggregator = App.Current.Container.Resolve<IEventAggregator>();
        token1 = eventAggregator.GetEvent<FriendRelationSelectEvent>().Subscribe(SelectFriendRelationEvent);
        token2 = eventAggregator.GetEvent<GroupRelationSelectEvent>().Subscribe(SelectGroupRelationEvent);
        token3 = eventAggregator.GetEvent<MoveToRelationEvent>().Subscribe(MoveToRelation);
    }

    private async void MoveToRelation(object obj)
    {
        if (obj is FriendRelationDto friendRelationDto)
        {
            TabControl.SelectedIndex = 0;
            await Task.Delay(50);
            SelectFriendRelationEvent(friendRelationDto);
        }
        else if (obj is GroupRelationDto groupRelationDto)
        {
            TabControl.SelectedIndex = 1;
            await Task.Delay(50);
            SelectGroupRelationEvent(groupRelationDto);
        }
    }

    private void SelectGroupRelationEvent(GroupRelationDto obj) =>
        GroupView.SelectItem(obj);

    private void SelectFriendRelationEvent(FriendRelationDto obj) =>
        FriendView.SelectItem(obj);

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        // GroupView.ClearAllSelected();
        // FriendView.ClearAllSelected();
        isLeftMovable = false;
        isHide = false;
    }

    private void PART_AddButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            // 先关闭已存在的菜单
            if (_activeContextMenu != null)
            {
                _activeContextMenu.Close();
                _activeContextMenu = null;
            }

            _activeContextMenu = new ContextMenu();

            // 创建群聊选项
            var createGroupItem = new MenuItem
            {
                Header = "创建群聊",
                Icon = new MaterialIcon
                    { Kind = MaterialIconKind.CommentPlusOutline },
                Command = ((ContactsViewModel)DataContext!).CreateGroupCommand
            };
            _activeContextMenu.Items.Add(createGroupItem);

            // 添加好友/群
            var addFriendItem = new MenuItem
            {
                Header = "添加好友/群",
                Icon = new MaterialIcon
                    { Kind = MaterialIconKind.AccountPlusOutline },
                Command = ((ContactsViewModel)DataContext!).AddNewFriendCommand
            };
            _activeContextMenu.Items.Add(addFriendItem);

            _activeContextMenu.Placement = PlacementMode.BottomEdgeAlignedLeft;
            _activeContextMenu.HorizontalOffset = 15;
            _activeContextMenu.PlacementTarget = button;

            _activeContextMenu.Open(button);
        }
    }

    private void FriendSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        GroupView.ClearAllSelected();
    }

    private void GroupSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        FriendView.ClearAllSelected();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        GroupView.ClearAllSelected();
        FriendView.ClearAllSelected();
    }

    public void Destroy()
    {
        token1.Dispose();
        token2.Dispose();
        token3.Dispose();
    }

    private void SearchScrollViewer_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
    }

    private void LeftSearchView_OnCardClick(object? sender, RoutedEventArgs e)
    {
        SearchBox.SearchText = null;
    }

    private bool isHide = false;
    private bool isLeftMovable = false;

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);

        if (e.NewSize.Width < 450 && !isHide)
        {
            isHide = true;

            Root.ColumnDefinitions[0].MaxWidth = 500;
            Root.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Pixel);
            Root.ColumnDefinitions[2].Width = new GridLength(0, GridUnitType.Pixel);
            Root.ColumnDefinitions[2].MinWidth = 0;
            //ContentControl.IsVisible = false;
            ContentControl.Opacity = 0;

            return;
        }

        if (e.NewSize.Width >= 460 && isHide)
        {
            isHide = false;

            Root.ColumnDefinitions[0].MaxWidth = 270;
            Root.ColumnDefinitions[1].Width = new GridLength(1.2, GridUnitType.Pixel);
            Root.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
            Root.ColumnDefinitions[2].MinWidth = 300;
            //ContentControl.IsVisible = true;
            ContentControl.Opacity = 1;

            return;
        }

        if (ContentControl.Bounds.Width <= 302 && !isLeftMovable)
        {
            isLeftMovable = true;

            Root.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            Root.ColumnDefinitions[0].MaxWidth = 270;

            return;
        }

        if (ContentControl.Bounds.Width > 310 && isLeftMovable)
        {
            isLeftMovable = false;

            Root.ColumnDefinitions[0].Width = new GridLength(270, GridUnitType.Pixel);
            Root.ColumnDefinitions[0].MaxWidth = 330;
            return;
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        var width = Bounds.Width;
        if (width < 100) return;

        if (ContentControl.Bounds.Width <= 302)
        {
            isLeftMovable = true;

            Root.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            Root.ColumnDefinitions[0].MaxWidth = 270;
        }

        if (ContentControl.Bounds.Width > 310)
        {
            isLeftMovable = false;

            Root.ColumnDefinitions[0].Width = new GridLength(270, GridUnitType.Pixel);
            Root.ColumnDefinitions[0].MaxWidth = 330;
        }

        if (width < 450)
        {
            isHide = true;

            Root.ColumnDefinitions[0].MaxWidth = 500;
            Root.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Pixel);
            Root.ColumnDefinitions[2].Width = new GridLength(0, GridUnitType.Pixel);
            Root.ColumnDefinitions[2].MinWidth = 0;
            ContentControl.Opacity = 0;
        }
        else if (width >= 460)
        {
            isHide = false;

            Root.ColumnDefinitions[0].MaxWidth = 270;
            Root.ColumnDefinitions[1].Width = new GridLength(1.2, GridUnitType.Pixel);
            Root.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
            Root.ColumnDefinitions[2].MinWidth = 300;
            ContentControl.Opacity = 1;
        }
    }
}