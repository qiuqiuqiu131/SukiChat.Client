using ChatClient.DataBase.EfCore.Data;
using ChatClient.DataBase.EfCore.EFCoreDB.UnitOfWork;

namespace ChatClient.DataBase.EfCore.EFCoreDB.Repository;

public class ChatGroupFileRepository : Repository<ChatGroupFile>
{
    public ChatGroupFileRepository(ChatClientDbContext dbContext) : base(dbContext)
    {
    }
}