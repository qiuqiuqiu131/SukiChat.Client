using ChatClient.DataBase.Data;
using ChatClient.DataBase.Repository;
using ChatClient.DataBase.UnitOfWork;
using ChatServer.DataBase.UnitOfWork;
using Prism.Ioc;

namespace ChatClient.DataBase;

public static class DataBaseExtensions
{
    public static void RegisterDataBase(this IContainerRegistry containerRegistry)
    {
        // 注册Sqlite
        containerRegistry.RegisterScoped<ChatClientDbContext>();

        // 注册工作单元
        containerRegistry.AddUnitOfWork<ChatClientDbContext>();

        // 注册仓储
        containerRegistry.AddCustomRepository<LoginHistory, LoginHistoryRepository>();
        containerRegistry.AddCustomRepository<User, UserRepository>();
        containerRegistry.AddCustomRepository<ChatPrivate, ChatPrivateRepository>();
        containerRegistry.AddCustomRepository<FriendRelation, FriendRelationRepository>();
        containerRegistry.AddCustomRepository<FriendRequest, FriendRequestRepository>();
        containerRegistry.AddCustomRepository<FriendDelete, FriendDeleteRepository>();

        containerRegistry.AddCustomRepository<Group, GroupRepository>();
        containerRegistry.AddCustomRepository<GroupRequest, GroupRequestRepository>();
        containerRegistry.AddCustomRepository<GroupRelation, GroupRelationRepository>();
        containerRegistry.AddCustomRepository<ChatGroup, ChatGroupRepository>();
        containerRegistry.AddCustomRepository<GroupDelete, GroupDeleteRepository>();
    }
}