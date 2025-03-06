using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.DataBase.Repository;

public class LoginHistoryRepository:Repository<LoginHistory>
{
    public LoginHistoryRepository(ChatClientDbContext dbContext) : base(dbContext)
    {
    }
}