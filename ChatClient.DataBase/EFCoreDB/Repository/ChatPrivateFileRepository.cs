using ChatClient.DataBase.Data;
using ChatClient.DataBase.EFCoreDB.UnitOfWork;

namespace ChatClient.DataBase.EFCoreDB.Repository;

public class ChatPrivateFileRepository : Repository<ChatPrivateFile>
{
    public ChatPrivateFileRepository(ChatClientDbContext dbContext) : base(dbContext)
    {
    }
}