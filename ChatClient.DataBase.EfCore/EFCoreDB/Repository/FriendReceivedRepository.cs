using ChatClient.DataBase.EfCore.Data;
using ChatClient.DataBase.EfCore.EFCoreDB.UnitOfWork;

namespace ChatClient.DataBase.EfCore.EFCoreDB.Repository;

public class FriendReceivedRepository : Repository<FriendReceived>
{
    public FriendReceivedRepository(ChatClientDbContext dbContext) : base(dbContext)
    {
    }
}