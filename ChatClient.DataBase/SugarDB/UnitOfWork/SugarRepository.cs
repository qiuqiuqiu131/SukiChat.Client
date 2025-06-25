using SqlSugar;

namespace ChatClient.DataBase.SugarDB.UnitOfWork;

/// <summary>
/// 通过依赖注入获取的自定义仓储类
/// </summary>
/// <typeparam name="T"></typeparam>
public class SugarRepository<T>(ISqlSugarClient db) : SimpleClient<T>(db) where T : class, new()
{
    // You can add custom methods here if needed
}