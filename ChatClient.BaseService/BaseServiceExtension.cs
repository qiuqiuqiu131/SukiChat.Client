using System.Runtime.InteropServices;
using AutoMapper;
using ChatClient.BaseService.Helper;
using ChatClient.BaseService.Helper.Linux;
using ChatClient.BaseService.Helper.Windows;
using ChatClient.BaseService.Manager;
using ChatClient.BaseService.Mapper;
using ChatClient.BaseService.MessageHandler;
using ChatClient.BaseService.Services;
using ChatClient.BaseService.Services.Interface;
using ChatClient.BaseService.Services.Interface.PackService;
using ChatClient.BaseService.Services.Interface.RemoteService;
using ChatClient.BaseService.Services.Interface.SearchService;
using ChatClient.BaseService.Services.ServiceEfCore.PackService;
using ChatClient.BaseService.Services.ServiceEfCore.RemoteService;
using ChatClient.BaseService.Services.ServiceEfCore.SearchService;
using ChatClient.BaseService.Services.ServiceSugar;
using ChatClient.BaseService.Services.ServiceSugar.PackService;
using ChatClient.BaseService.Services.ServiceSugar.RemoteService;
using ChatClient.BaseService.Services.ServiceSugar.SearchServie;
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
        containerRegistry.Register<ILoginService, LoginSugarService>() // 
            .Register<IFriendService, FriendSugarService>() // 
            .Register<IUserService, UserSugarService>() //
            .Register<IChatService, ChatSugarService>() //
            .Register<IGroupGetService, GroupGetSugarService>() //
            .Register<IGroupService, GroupSugarService>() //
            .Register<IUserGroupService, UserGroupSugarService>() // 
            .Register<IPasswordService, PasswordService>() // -
            .Register<IRegisterService, RegisterService>() // -
            .Register<IChatLRService, ChatLRService>(); // -

        // 注册SearchService
        containerRegistry.Register<ISearchService, SearchService>() // -
            .Register<ILocalSearchService, LocalSearchSugarService>(); //

        // 注册PackService
        containerRegistry.Register<IFriendChatPackService, FriendChatSugarPackService>() //
            .Register<IFriendPackService, FriendSugarPackService>() //
            .Register<IGroupPackService, GroupSugarPackService>() //
            .Register<IGroupChatPackService, GroupChatSugarPackService>() //
            .Register<IUserLoginService, UserLoginService>(); // -

        // 注册RemoteService
        containerRegistry.Register<IGroupRemoteService, GroupSugarRemoteService>() //
            .Register<IUserRemoteService, UserSugarRemoteService>() // 
            .Register<IChatRemoteService, ChatRemoteService>(); // -

        // 注册MessageHandler
        containerRegistry.Register<IMessageHandler, FriendMessageHandler>()
            .Register<IMessageHandler, ChatMessageHandler>()
            .Register<IMessageHandler, FriendLogInOutMessageHandler>()
            .Register<IMessageHandler, GroupMessageHandler>()
            .Register<IMessageHandler, GroupRelationMessageHandler>()
            .Register<IMessageHandler, DeleteMessageHandler>()
            .Register<IMessageHandler, CallMessageHandler>(); // -
    }
}