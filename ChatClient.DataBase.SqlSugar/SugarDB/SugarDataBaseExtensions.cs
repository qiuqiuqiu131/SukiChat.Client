using ChatClient.DataBase.SqlSugar.Data;
using ChatClient.DataBase.SqlSugar.SugarDB.UnitOfWork;
using ChatClient.Tool.ManagerInterface;
using SqlSugar;

namespace ChatClient.DataBase.SqlSugar.SugarDB;

public static class SugarDataBaseExtensions
{
    public static void EnsureDbCreated(this IContainerProvider containerProvider)
    {
        var dbPath = containerProvider.Resolve<IAppDataManager>().GetFileInfo("ChatClient.db").FullName;
        var db = new SqlSugarClient(new ConnectionConfig
        {
            DbType = DbType.Sqlite,
            ConnectionString = $"Data Source={dbPath};",
            IsAutoCloseConnection = true
        });

        var types = GetDbTypes();

        // 检查 groupRequest / groupRequests 表是否存在字段 ID
        var tableNames = db.DbMaintenance.GetTableInfoList().Select(d => d.Name).ToList();
        var targetTable = tableNames.FirstOrDefault(n =>
            n.Equals("groupRequest", System.StringComparison.OrdinalIgnoreCase)
            || n.Equals("groupRequests", System.StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(targetTable))
        {
            var columns = db.DbMaintenance.GetColumnInfosByTableName(targetTable);
            var hasId = columns.Any(c => c.DbColumnName.Equals("ID", System.StringComparison.OrdinalIgnoreCase)
                                         || c.DbColumnName.Equals("Id", System.StringComparison.OrdinalIgnoreCase));
            if (hasId)
            {
                var allTables = db.DbMaintenance.GetTableInfoList().Select(t => t.Name);
                db.DbMaintenance.DropTable(allTables.ToArray());

                // 重新初始化 GroupRequest 表以确保包含 ID 字段
                db.CodeFirst.SetStringDefaultLength(200).InitTables(types);
            }
        }

        if (db.DbMaintenance.GetTableInfoList().Count != types.Length)
            db.CodeFirst.SetStringDefaultLength(200).InitTables(types);

        // 重命名表名，确保所有表名都以"s"结尾
        foreach (var tableName in db.DbMaintenance.GetTableInfoList().Select(d => d.Name).ToList())
            if (!tableName.EndsWith('s'))
                db.DbMaintenance.RenameTable($"`{tableName}`", $"`{tableName}s`");

        db.Dispose();
    }

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