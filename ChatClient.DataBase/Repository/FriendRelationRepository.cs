using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.DataBase.Repository;

public class FriendRelationRepository : Repository<FriendRelation>
{
    public FriendRelationRepository(ChatClientDbContext dbContext) : base(dbContext){}
}