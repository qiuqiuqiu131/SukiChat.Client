using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;

namespace ChatClient.DataBase.Repository;

public class ChatGroupRepository : Repository<ChatGroup>
{
    public ChatGroupRepository(ChatClientDbContext dbContext) : base(dbContext)
    {
    }
}