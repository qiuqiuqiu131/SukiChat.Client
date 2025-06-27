using System.Runtime.InteropServices;
using ChatClient.BaseService.CallOperator;
using ChatClient.BaseService.Helper;
using ChatClient.BaseService.Helper.Linux;
using ChatClient.BaseService.Helper.Windows;
using ChatClient.BaseService.Manager;
using ChatClient.BaseService.MessageHandler;
using ChatClient.BaseService.Services;
using ChatClient.BaseService.Services.Interface;
using ChatClient.BaseService.Services.Interface.PackService;
using ChatClient.BaseService.Services.Interface.RemoteService;
using ChatClient.BaseService.Services.Interface.SearchService;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.Media.Call;

namespace ChatClient.BaseService;

public static class BaseServiceExtension
{
    public static void RegisterBaseServices(this IContainerRegistry containerRegistry)
    {
        // 注册单例
        containerRegistry.RegisterSingleton<IConnection, ConnectionManager>()
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
                .Register<ISystemFileDialog, WindowsFileDialog>();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            containerRegistry
                .Register<ISystemFileDialog, LinuxFileDialog>()
                .Register<ITaskbarFlashHelper, LinuxTaskbarFlashHelper>()
                .Register<ISystemScalingHelper, LinuxScalingHelper>();
        }

        // 注册Service
        containerRegistry.Register<IPasswordService, PasswordService>() // -
            .Register<IRegisterService, RegisterService>() // -
            .Register<IChatLRService, ChatLRService>(); // -

        // 注册SearchService
        containerRegistry.Register<ISearchService, SearchService>(); // -

        // 注册PackService
        containerRegistry.Register<IUserLoginService, UserLoginService>(); // -

        // 注册RemoteService
        containerRegistry.Register<IChatRemoteService, ChatRemoteService>(); // -

        // 注册MessageHandler
        containerRegistry.Register<IMessageHandler, FriendMessageHandler>()
            .Register<IMessageHandler, ChatMessageHandler>()
            .Register<IMessageHandler, FriendLogInOutMessageHandler>()
            .Register<IMessageHandler, GroupMessageHandler>()
            .Register<IMessageHandler, GroupRelationMessageHandler>()
            .Register<IMessageHandler, DeleteMessageHandler>()
            .Register<IMessageHandler, CallMessageHandler>(); // -

        // 注册通话
        containerRegistry.RegisterSingleton<CallManager>()
            .RegisterSingleton<ICallManager>(d => d.Resolve<CallManager>())
            .RegisterSingleton<ICallOperator>(d => d.Resolve<CallManager>());

        // 注册通话处理器
        containerRegistry.Register<TelephoneCallOperator>()
            .Register<VideoCallOperator>();
    }
}