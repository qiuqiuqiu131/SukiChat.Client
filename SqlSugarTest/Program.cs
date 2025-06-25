using ChatClient.DataBase.Data;
using ChatClient.DataBase.SugarDB;
using ChatClient.DataBase.SugarDB.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace SqlSugarTest;

class Program
{
    static async Task Main(string[] args)
    {
        StaticConfig.EnableAot = true;

        string dbPath = "D:\\ChatClient.db";

        var builder = new ServiceCollection();

        builder.AddScoped<ISugarUnitOfWork<SugarChatClientDbContext>>(d =>
        {
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
                        .InitTables(SugarDataBaseExtensions.GetDbTypes());
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            return new SugarUnitOfWork<SugarChatClientDbContext>(db);
        });

        var serviceProvider = builder.BuildServiceProvider();

        // 1 : 通过依赖注入获取自定义仓储
        // var userRepository = serviceProvider.GetRequiredService<SugarRepository<User>>();
        // var groupRepository = serviceProvider.GetRequiredService<SugarRepository<GroupMember>>();
        // var user = await userRepository.GetFirstAsync(d => d.isMale);
        // var members = await groupRepository.GetPageListAsync(d => d.UserId == user.Id,
        //     new PageModel { PageIndex = 1, PageSize = 20 },
        //     d => d.JoinTime, OrderByType.Desc);

        // 2 : 工作单元模型，有泛型
        var unitOfWork = serviceProvider.GetRequiredService<ISugarUnitOfWork<SugarChatClientDbContext>>();
        try
        {
            using (var uow = unitOfWork.CreateContext())
            {
                var userRepository = uow.GetRepository<User>();

                // 获取自定义仓储的两种方法
                var groupRepository1 = uow.GetMyRepository<SugarDbSet<GroupMember>>();
                var groupRepository2 = uow.GroupMember;
                var user = await userRepository.GetListAsync();

                uow.Commit();
            }
        }
        catch (Exception e)
        {
            // 回滚事务
            await unitOfWork.Db.Ado.RollbackTranAsync();

            Console.WriteLine(e);
            throw;
        }

        // 3 : 工作单元模型，没有泛型
        // var db = serviceProvider.GetRequiredService<ISqlSugarClient>();
        // using var uow = db.CreateContext();
        // var userRepository = uow.GetRepository<User>();
        // var users = await userRepository.GetListAsync();
    }
}