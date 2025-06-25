using ChatClient.DataBase.Data;
using ChatClient.DataBase.EFCoreDB.UnitOfWork;

namespace ChatClient.DataBase.EFCoreDB.Repository;

public class ChatGroupRepository : Repository<ChatGroup>
{
    public ChatGroupRepository(ChatClientDbContext dbContext) : base(dbContext)
    {
    }
}