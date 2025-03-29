using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.DataBase.Repository;

public class ChatPrivateFileRepository : Repository<ChatPrivateFile>
{
    public ChatPrivateFileRepository(ChatClientDbContext dbContext) : base(dbContext)
    {
    }
}