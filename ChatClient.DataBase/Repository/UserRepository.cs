using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;

namespace ChatClient.DataBase.Repository;

public class UserRepository:Repository<User>
{
    public UserRepository(ChatClientDbContext dbContext) : base(dbContext)
    {
    }
}