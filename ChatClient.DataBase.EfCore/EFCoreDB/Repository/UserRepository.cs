using ChatClient.DataBase.EfCore.Data;
using ChatClient.DataBase.EfCore.EFCoreDB.UnitOfWork;

namespace ChatClient.DataBase.EfCore.EFCoreDB.Repository;

public class UserRepository : Repository<User>
{
    public UserRepository(ChatClientDbContext dbContext) : base(dbContext)
    {
    }
}