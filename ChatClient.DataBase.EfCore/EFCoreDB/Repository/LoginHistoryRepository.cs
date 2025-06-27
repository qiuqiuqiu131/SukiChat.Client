using ChatClient.DataBase.EfCore.Data;
using ChatClient.DataBase.EfCore.EFCoreDB.UnitOfWork;

namespace ChatClient.DataBase.EfCore.EFCoreDB.Repository;

public class LoginHistoryRepository : Repository<LoginHistory>
{
    public LoginHistoryRepository(ChatClientDbContext dbContext) : base(dbContext)
    {
    }
}