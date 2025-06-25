using ChatClient.DataBase.Data;
using ChatClient.DataBase.EFCoreDB.UnitOfWork;

namespace ChatClient.DataBase.EFCoreDB.Repository;

public class FriendRelationRepository : Repository<FriendRelation>
{
    public FriendRelationRepository(ChatClientDbContext dbContext) : base(dbContext)
    {
    }
}