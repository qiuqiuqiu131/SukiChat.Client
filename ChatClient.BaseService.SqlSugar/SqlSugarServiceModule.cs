using AutoMapper;
using ChatClient.BaseService.Services;
using ChatClient.BaseService.Services.Interface;
using ChatClient.BaseService.Services.Interface.PackService;
using ChatClient.BaseService.Services.Interface.RemoteService;
using ChatClient.BaseService.Services.Interface.SearchService;
using ChatClient.BaseService.SqlSugar.Mapper;
using ChatClient.BaseService.SqlSugar.PackService;
using ChatClient.BaseService.SqlSugar.RemoteService;
using ChatClient.BaseService.SqlSugar.SearchServie;

namespace ChatClient.BaseService.SqlSugar;

public class SqlSugarServiceModule : IModule
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
        containerRegistry.Register<ILoginService, LoginSugarService>() // 
            .Register<IFriendService, FriendSugarService>() // 
            .Register<IUserService, UserSugarService>() //
            .Register<IChatService, ChatSugarService>() //
            .Register<IGroupGetService, GroupGetSugarService>() //
            .Register<IGroupService, GroupSugarService>() //
            .Register<IUserGroupService, UserGroupSugarService>(); //

        // 注册SearchService
        containerRegistry.Register<ILocalSearchService, LocalSearchSugarService>(); //

        // 注册PackService
        containerRegistry.Register<IFriendChatPackService, FriendChatSugarPackService>() //
            .Register<IFriendPackService, FriendSugarPackService>() //
            .Register<IGroupPackService, GroupSugarPackService>() //
            .Register<IGroupChatPackService, GroupChatSugarPackService>(); //

        // 注册RemoteService
        containerRegistry.Register<IGroupRemoteService, GroupSugarRemoteService>() //
            .Register<IUserRemoteService, UserSugarRemoteService>(); //
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
    }
}