using ChatClient.DataBase.Data;
using ChatClient.DataBase.EFCoreDB.UnitOfWork;

namespace ChatClient.DataBase.EFCoreDB.Repository;

public class UserRepository : Repository<User>
{
    public UserRepository(ChatClientDbContext dbContext) : base(dbContext)
    {
    }
}