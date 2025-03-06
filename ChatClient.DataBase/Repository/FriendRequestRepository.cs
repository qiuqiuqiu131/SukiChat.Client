using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.DataBase.Repository;

public class FriendRequestRepository : Repository<FriendRequest>
{
    public FriendRequestRepository(ChatClientDbContext dbContext) : base(dbContext)
    {
    }
}