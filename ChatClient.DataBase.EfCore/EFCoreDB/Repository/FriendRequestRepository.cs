using ChatClient.DataBase.EfCore.Data;
using ChatClient.DataBase.EfCore.EFCoreDB.UnitOfWork;

namespace ChatClient.DataBase.EfCore.EFCoreDB.Repository;

public class FriendRequestRepository : Repository<FriendRequest>
{
    public FriendRequestRepository(ChatClientDbContext dbContext) : base(dbContext)
    {
    }
}