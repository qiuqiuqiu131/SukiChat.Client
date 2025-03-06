using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.DataBase.Repository;

public class FriendReceivedRepository : Repository<FriendReceived>
{
    public FriendReceivedRepository(ChatClientDbContext dbContext) : base(dbContext)
    {
    }
}