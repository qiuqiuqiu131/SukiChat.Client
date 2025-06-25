using ChatClient.DataBase.Data;
using ChatClient.DataBase.EFCoreDB.UnitOfWork;

namespace ChatClient.DataBase.EFCoreDB.Repository;

public class FriendReceivedRepository : Repository<FriendReceived>
{
    public FriendReceivedRepository(ChatClientDbContext dbContext) : base(dbContext)
    {
    }
}