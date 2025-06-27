using ChatClient.DataBase.EfCore.Data;
using ChatClient.DataBase.EfCore.EFCoreDB.UnitOfWork;

namespace ChatClient.DataBase.EfCore.EFCoreDB.Repository;

public class ChatPrivateFileRepository : Repository<ChatPrivateFile>
{
    public ChatPrivateFileRepository(ChatClientDbContext dbContext) : base(dbContext)
    {
    }
}