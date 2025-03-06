using System.Collections.ObjectModel;
using AutoMapper;
using Avalonia.Collections;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ChatClient.BaseService.Helper;
using ChatClient.BaseService.MessageHandler;
using ChatClient.BaseService.Services;
using ChatClient.BaseService.Services.PackService;
using ChatClient.DataBase;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.Tools;
using ChatServer.Common.Protobuf;
using DryIoc.ImTools;
using Microsoft.EntityFrameworkCore;
using SukiUI.Toasts;

namespace ChatClient.BaseService.Manager;

internal class UserManager : IUserManager
{
    public bool IsLogin { get; private set; } = false;

    #region UserData(用户所有的数据)

    // 主体数据
    public UserData? UserData { get; private set; }

    // 信息属性
    public UserDto? User => UserData?.UserDetail;
    public AvaloniaList<FriendReceiveDto>? FriendReceives => UserData?.FriendReceives;
    public AvaloniaList<GroupFriendDto>? GroupFriends => UserData?.GroupFriends;
    public AvaloniaList<FriendChatDto>? FriendChats => UserData?.FriendChatDtos;

    #endregion

    private readonly IEventAggregator _eventAggregator;
    private readonly IFileOperateHelper _fileOperateHelper;
    private readonly IUserDtoManager _userDtoManager;
    private readonly IContainerProvider _containerProvider;

    public UserManager(
        IEventAggregator eventAggregator,
        IFileOperateHelper fileOperateHelper,
        IUserDtoManager userDtoManager,
        IContainerProvider containerProvider)
    {
        _eventAggregator = eventAggregator;
        _fileOperateHelper = fileOperateHelper;
        _userDtoManager = userDtoManager;
        _containerProvider = containerProvider;
    }

    // 用户登录请求调用，返回登录的用户数据
    public async Task<CommonResponse?> Login(string id, string password, bool isRemember = false)
    {
        var loginService = _containerProvider.Resolve<ILoginService>();
        var response = await loginService.Login(id, password, isRemember);

        if (!(response is { State: true })) return null;

        // 调用userService获取用户完整数据
        var _userService = _containerProvider.Resolve<IUserLoginService>();
        UserData = await _userService.GetUserFullData(id);

        // 登录成功
        await loginService.LoginSuccess(UserData.UserDetail);

        IsLogin = true;
        // 登录成功后，程序启用全双工通信，开始监听消息。接收到消息后，由eventaggregator发布消息。 
        RegisterEvent(_eventAggregator);
        return response;
    }

    /// <summary>
    /// 用户登出请求调用
    /// </summary>
    public async Task<CommonResponse?> Logout()
    {
        var loginService = _containerProvider.Resolve<ILoginService>();
        var response = await loginService.Logout(UserData?.UserDetail.Id);

        IsLogin = false;
        // 登出后，取消监听消息
        UnRegisterEvent(_eventAggregator);

        // 清空用户数据
        UserData?.Clear();
        UserData = null;
        _userDtoManager.Clear();

        return response;
    }

    /// <summary>
    /// 添加头像
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public async Task<bool> ResetHead(Bitmap? bitmap)
    {
        if (bitmap == null || User == null) return false;

        var path = Path.Combine(User.Id, "HeadImage");
        var fileName = $"head_{User.HeadIndex}.png";
        byte[] bytes = bitmap.BitmapToByteArray();
        var result = await _fileOperateHelper.UploadFileForUser(path, fileName, bytes);
        if (!result) return false;

        User.HeadCount++;
        User.HeadIndex = User.HeadCount - 1;
        User.HeadImage = bitmap;

        await SaveUser();
        return true;
    }

    /// <summary>
    /// 在更改用户信息后保存用户，必须调用一下
    /// </summary>
    public async Task SaveUser()
    {
        var userService = _containerProvider.Resolve<IUserService>();
        await userService.SaveUser(User);
    }

    /// <summary>
    /// 当添加了新朋友时，更新朋友列表
    /// </summary>
    /// <param name="friendId"></param>
    /// <param name="dto"></param>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<FriendRelationDto?> NewFriendRecieve(string friendId)
    {
        var dto = await _userDtoManager.GetFriendRelationDto(User.Id, friendId);
        if (dto != null)
        {
            var groupFriend = GroupFriends.FirstOrDefault(d => d.GroupName.Equals(dto.Grouping));
            if (groupFriend != null)
                groupFriend.Friends.Add(dto);
            else
                GroupFriends.Add(new GroupFriendDto
                {
                    GroupName = dto.Grouping,
                    Friends = new AvaloniaList<FriendRelationDto> { dto }
                });

            FriendChats!.Add(new FriendChatDto
            {
                FriendRelatoinDto = dto,
                UserId = dto.Id
            });
        }

        return dto;
    }

    #region EventRegister

    // 用于存储所有的消息处理器
    private List<IMessageHandler>? handlers;

    private void RegisterEvent(IEventAggregator eventAggregator)
    {
        handlers = _containerProvider.Resolve<IEnumerable<IMessageHandler>>().ToList();
        foreach (var handler in handlers)
            handler.RegisterEvent(eventAggregator);
    }

    private void UnRegisterEvent(IEventAggregator eventAggregator)
    {
        foreach (var handler in handlers)
            handler.UnRegisterEvent(eventAggregator);
        handlers.Clear();
        handlers = null;
    }

    #endregion
}