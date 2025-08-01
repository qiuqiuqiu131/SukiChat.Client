using ChatClient.DataBase.SqlSugar.Data;
using ChatClient.DataBase.SqlSugar.SugarDB.UnitOfWork;
using ChatClient.Tool.ManagerInterface;
using SqlSugar;

namespace ChatClient.DataBase.SqlSugar.SugarDB;

public static class SugarDataBaseExtensions
{
    public static void RegisterSugarDataBase(this IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterScoped<ISqlSugarClient>(d =>
        {
            var dbPath = d.Resolve<IAppDataManager>().GetFileInfo("ChatClient.db").FullName;
            var db = new SqlSugarClient(new ConnectionConfig
            {
                DbType = DbType.Sqlite,
                ConnectionString = $"Data Source={dbPath};",
                IsAutoCloseConnection = true
            }, i =>
            {
                i.Aop.OnLogExecuted = (s, p) =>
                {
                    // Console.WriteLine(s);
                };
            });

            // 创建数据库表
            if (!System.IO.File.Exists(dbPath) || db.DbMaintenance.GetTableInfoList(false).Count != GetDbTypes().Length)
            {
                try
                {
                    db.CodeFirst.SetStringDefaultLength(200)
                        .InitTables(GetDbTypes());
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            return db;
        });
    }

    public static void RegisterSugarDataBaseFull(this IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterScoped<ISugarUnitOfWork<SugarChatClientDbContext>>(d =>
        {
            var dbPath = d.Resolve<IAppDataManager>().GetFileInfo("ChatClient.db").FullName;
            var db = new SqlSugarClient(new ConnectionConfig
            {
                DbType = DbType.Sqlite,
                ConnectionString = $"Data Source={dbPath};",
                IsAutoCloseConnection = true
            });

            // 创建数据库表
            if (!System.IO.File.Exists(dbPath))
            {
                try
                {
                    db.CodeFirst.SetStringDefaultLength(200)
                        .InitTables(GetDbTypes());
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            return new SugarUnitOfWork<SugarChatClientDbContext>(db);
        });
    }

    /// <summary>
    /// 编写了自定义的SugarRepository注册方法
    /// </summary>
    /// <param name="containerRegistry"></param>
    public static void RegisterSugarMyRepository(this IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterScoped<SugarRepository<User>>()
            .RegisterScoped<SugarRepository<LoginHistory>>()
            .RegisterScoped<SugarRepository<GroupRequest>>()
            .RegisterScoped<SugarRepository<GroupRelation>>()
            .RegisterScoped<SugarRepository<GroupReceived>>()
            .RegisterScoped<SugarRepository<GroupMember>>()
            .RegisterScoped<SugarRepository<GroupDelete>>()
            .RegisterScoped<SugarRepository<Group>>()
            .RegisterScoped<SugarRepository<FriendRequest>>()
            .RegisterScoped<SugarRepository<FriendRelation>>()
            .RegisterScoped<SugarRepository<FriendReceived>>()
            .RegisterScoped<SugarRepository<FriendDelete>>()
            .RegisterScoped<SugarRepository<ChatPrivateFile>>()
            .RegisterScoped<SugarRepository<ChatPrivateDetail>>()
            .RegisterScoped<SugarRepository<ChatPrivate>>()
            .RegisterScoped<SugarRepository<ChatGroupFile>>()
            .RegisterScoped<SugarRepository<ChatGroupDetail>>()
            .RegisterScoped<SugarRepository<ChatGroup>>();
    }

    public static Type[] GetDbTypes()
    {
        return
        [
            typeof(User), typeof(LoginHistory), typeof(GroupRequest), typeof(GroupRelation),
            typeof(GroupReceived), typeof(GroupMember), typeof(GroupDelete), typeof(Group),
            typeof(FriendRequest), typeof(FriendRelation), typeof(FriendReceived), typeof(FriendDelete),
            typeof(ChatPrivateFile), typeof(ChatPrivateDetail), typeof(ChatPrivate), typeof(ChatGroupFile),
            typeof(ChatGroupDetail), typeof(ChatGroup)
        ];
    }
}