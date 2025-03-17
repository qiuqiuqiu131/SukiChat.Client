using AutoMapper;
using ChatClient.BaseService.Helper;
using ChatClient.BaseService.Manager;
using ChatClient.BaseService.Mapper;
using ChatClient.BaseService.MessageHandler;
using ChatClient.BaseService.Services;
using ChatClient.BaseService.Services.PackService;
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

        // 注册Manager
        containerRegistry.RegisterSingleton<IAppDataManager, AppDataManager>()
            .RegisterSingleton<IConnection, ConnectionManager>()
            .RegisterSingleton<IThemeStyle, ThemeStyleManager>()
            .Register<ILoginData, LoginDataManager>();

        // 注册UserManager
        containerRegistry.RegisterSingleton<IUserManager, UserManager>()
            .RegisterSingleton<IUserDtoManager, UserDtoManager>();

        // 注册Helper
        containerRegistry.Register<IMessageHelper, MessageHelper>()
            .Register<IFileIOHelper, ProtoFileIOHelper>()
            .Register<IFileOperateHelper, FileOperateHelper>()
            .Register<ISystemScalingHelper, WindowScalingHelper>();

        // 注册Service
        containerRegistry.Register<ILoginService, LoginService>()
            .Register<IRegisterService, RegisterService>()
            .Register<IFriendService, FriendService>()
            .Register<IUserService, UserService>()
            .Register<IUserLoginService, UserLoginService>()
            .Register<IChatService, ChatService>()
            .Register<IGroupGetService, GroupGetService>()
            .Register<IGroupService, GroupService>();

        // 注册PackService
        containerRegistry.Register<IFriendChatPackService, FriendChatPackService>()
            .Register<IFriendPackService, FriendPackService>()
            .Register<IGroupPackService, GroupPackService>()
            .Register<IGroupChatPackService, GroupChatPackService>();

        // 注册MessageHandler
        containerRegistry.Register<IMessageHandler, FriendMessageHandler>()
            .Register<IMessageHandler, ChatMessageHandler>()
            .Register<IMessageHandler, FriendLogInOutMessageHandler>()
            .Register<IMessageHandler, GroupMessageHandler>()
            .Register<IMessageHandler, GroupRelationMessageHandler>()
            .Register<IMessageHandler, DeleteMessageHandler>();
    }
}