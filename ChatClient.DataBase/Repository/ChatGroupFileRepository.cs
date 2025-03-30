using ChatClient.DataBase.Data;

namespace ChatClient.DataBase.Repository;

using ChatClient.DataBase.UnitOfWork;

public class ChatGroupFileRepository : Repository<ChatGroupFile>
{
    public ChatGroupFileRepository(ChatClientDbContext dbContext) : base(dbContext)
    {
    }
}