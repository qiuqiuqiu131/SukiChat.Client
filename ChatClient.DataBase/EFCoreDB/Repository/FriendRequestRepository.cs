using ChatClient.DataBase.Data;
using ChatClient.DataBase.EFCoreDB.UnitOfWork;

namespace ChatClient.DataBase.EFCoreDB.Repository;

public class FriendRequestRepository : Repository<FriendRequest>
{
    public FriendRequestRepository(ChatClientDbContext dbContext) : base(dbContext)
    {
    }
}