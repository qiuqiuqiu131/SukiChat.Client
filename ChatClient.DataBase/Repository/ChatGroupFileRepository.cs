using ChatClient.DataBase.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.DataBase.Repository;

using ChatClient.DataBase.UnitOfWork;

public class ChatGroupFileRepository : Repository<ChatGroupFile>
{
    public ChatGroupFileRepository(ChatClientDbContext dbContext) : base(dbContext)
    {
    }
}