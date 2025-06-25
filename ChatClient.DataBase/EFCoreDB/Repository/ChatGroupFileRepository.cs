using ChatClient.DataBase.Data;
using ChatClient.DataBase.EFCoreDB.UnitOfWork;

namespace ChatClient.DataBase.EFCoreDB.Repository;

public class ChatGroupFileRepository : Repository<ChatGroupFile>
{
    public ChatGroupFileRepository(ChatClientDbContext dbContext) : base(dbContext)
    {
    }
}