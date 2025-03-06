using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.DataBase.Repository;

public class ChatPrivateRepository : Repository<ChatPrivate>
{
    public ChatPrivateRepository(DbContext dbContext) : base(dbContext){}
}