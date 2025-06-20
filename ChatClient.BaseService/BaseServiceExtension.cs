using System.Runtime.InteropServices;
using AutoMapper;
using ChatClient.BaseService.Helper;
using ChatClient.BaseService.Helper.Linux;
using ChatClient.BaseService.Manager;
using ChatClient.BaseService.Mapper;
using ChatClient.BaseService.MessageHandler;
using ChatClient.BaseService.Services;
using ChatClient.BaseService.Services.PackService;
using ChatClient.BaseService.Services.RemoteService;
using ChatClient.BaseService.Services.SearchService;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;

namespace ChatClient.BaseService;

public static class BaseServiceExtension
{
    public static void RegisterBaseServices(this IContainerRegistry containerRegistry)
    {
        // 注册Mapper
        var mapConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<DataToDtoProfile>();
            cfg.AddProfile<ProtoToDtoProfile>();
            cfg.AddProfile<ProtoToDataProfile>();
        }).CreateMapper();
        containerRegistry.RegisterInstance(mapConfig);

        // 注册单例
        containerRegistry.RegisterSingleton<IAppDataManager, AppDataManager>()
            .RegisterSingleton<IConnection, ConnectionManager>()
            .RegisterSingleton<IThemeStyle, ThemeStyleManager>()
            .RegisterSingleton<IUserSetting, UserSettingsManager>()
            .Register<ILoginData, LoginDataManager>();

        // 注册UserManager
        containerRegistry.RegisterSingleton<IUserManager, UserManager>()
            .RegisterSingleton<IImageManager, ImageManager>()
            .RegisterSingleton<IFileManager, FileManager>()
            .RegisterSingleton<IStunServerManager, StunServerManager>()
            .RegisterSingleton<IUserDtoManager, UserDtoManager>();

        // 注册Helper
        containerRegistry.Register<IMessageHelper, MessageHelper>()
            .Register<IFileIOHelper, ProtoFileIOHelper>()
            .Register<IFileOperateHelper, FileOperateHelper>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // 注册平台特定的Helper
            containerRegistry
                .Register<ITaskbarFlashHelper, WindowTaskbarFlashHelper>()
                .Register<ISystemScalingHelper, WindowScalingHelper>()
                .Register<ISystemFileDialog, WindowsFileDialog>()
                .Register<ISystemCaptureScreen, WindowsCaptureScreenHelper>();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            containerRegistry
                .Register<ISystemFileDialog, LinuxFileDialog>()
                .Register<ITaskbarFlashHelper, LinuxTaskbarFlashHelper>()
                .Register<ISystemScalingHelper, LinuxScalingHelper>()
                .Register<ISystemCaptureScreen, LinuxCaptureScreenHelper>();
        }

        // 注册Service
        containerRegistry.Register<ILoginService, LoginService>()
            .Register<IRegisterService, RegisterService>()
            .Register<IFriendService, FriendService>()
            .Register<IUserService, UserService>()
            .Register<IUserLoginService, UserLoginService>()
            .Register<IChatService, ChatService>()
            .Register<IGroupGetService, GroupGetService>()
            .Register<IGroupService, GroupService>()
            .Register<IUserGroupService, UserGroupService>()
            .Register<IPasswordService, PasswordService>()
            .Register<IChatLRService, ChatLRService>();

        // 注册SearchService
        containerRegistry.Register<ISearchService, SearchService>()
            .Register<ILocalSearchService, LocalSearchService>();

        // 注册PackService
        containerRegistry.Register<IFriendChatPackService, FriendChatPackService>()
            .Register<IFriendPackService, FriendPackService>()
            .Register<IGroupPackService, GroupPackService>()
            .Register<IGroupChatPackService, GroupChatPackService>();

        // 注册RemoteService
        containerRegistry.Register<IGroupRemoteService, GroupRemoteService>()
            .Register<IUserRemoteService, UserRemoteService>()
            .Register<IChatRemoteService, ChatRemoteService>();

        // 注册MessageHandler
        containerRegistry.Register<IMessageHandler, FriendMessageHandler>()
            .Register<IMessageHandler, ChatMessageHandler>()
            .Register<IMessageHandler, FriendLogInOutMessageHandler>()
            .Register<IMessageHandler, GroupMessageHandler>()
            .Register<IMessageHandler, GroupRelationMessageHandler>()
            .Register<IMessageHandler, DeleteMessageHandler>()
            .Register<IMessageHandler, CallMessageHandler>();
    }
}