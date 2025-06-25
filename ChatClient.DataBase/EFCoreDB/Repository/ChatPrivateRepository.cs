using ChatClient.DataBase.Data;
using ChatClient.DataBase.EFCoreDB.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.DataBase.EFCoreDB.Repository;

public class ChatPrivateRepository : Repository<ChatPrivate>
{
    public ChatPrivateRepository(DbContext dbContext) : base(dbContext)
    {
    }
}