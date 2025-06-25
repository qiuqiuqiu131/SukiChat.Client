using ChatClient.DataBase.Data;
using ChatClient.DataBase.EFCoreDB.UnitOfWork;

namespace ChatClient.DataBase.EFCoreDB.Repository;

public class LoginHistoryRepository : Repository<LoginHistory>
{
    public LoginHistoryRepository(ChatClientDbContext dbContext) : base(dbContext)
    {
    }
}