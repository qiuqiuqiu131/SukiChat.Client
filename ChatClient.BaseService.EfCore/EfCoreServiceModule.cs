using AutoMapper;
using ChatClient.BaseService.EfCore.Mapper;
using ChatClient.BaseService.EfCore.PackService;
using ChatClient.BaseService.EfCore.RemoteService;
using ChatClient.BaseService.EfCore.SearchService;
using ChatClient.BaseService.Services;
using ChatClient.BaseService.Services.Interface;
using ChatClient.BaseService.Services.Interface.PackService;
using ChatClient.BaseService.Services.Interface.RemoteService;
using ChatClient.BaseService.Services.Interface.SearchService;

namespace ChatClient.BaseService.EfCore;

public class EfCoreServiceModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册基础服务
        containerRegistry.RegisterBaseServices();

        // 注册Mapper
        var mapConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<DataToDtoProfile>();
            cfg.AddProfile<ProtoToDtoProfile>();
            cfg.AddProfile<ProtoToDataProfile>();
        }).CreateMapper();
        containerRegistry.RegisterInstance(mapConfig);

        // 注册Service
        containerRegistry.Register<ILoginService, LoginService>() // 
            .Register<IFriendService, FriendService>() // 
            .Register<IUserService, UserService>() //
            .Register<IChatService, ChatService>() //
            .Register<IGroupGetService, GroupGetService>() //
            .Register<IGroupService, GroupService>() //
            .Register<IUserGroupService, UserGroupService>(); // 

        // 注册SearchService
        containerRegistry.Register<ILocalSearchService, LocalSearchService>(); //

        // 注册PackService
        containerRegistry.Register<IFriendChatPackService, FriendChatPackService>() //
            .Register<IFriendPackService, FriendPackService>() //
            .Register<IGroupPackService, GroupPackService>() //
            .Register<IGroupChatPackService, GroupChatPackService>(); //

        // 注册RemoteService
        containerRegistry.Register<IGroupRemoteService, GroupRemoteService>() //
            .Register<IUserRemoteService, UserRemoteService>(); // 
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
    }
}