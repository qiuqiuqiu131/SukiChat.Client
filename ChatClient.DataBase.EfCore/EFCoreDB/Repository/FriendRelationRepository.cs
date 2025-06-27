using ChatClient.DataBase.EfCore.Data;
using ChatClient.DataBase.EfCore.EFCoreDB.UnitOfWork;

namespace ChatClient.DataBase.EfCore.EFCoreDB.Repository;

public class FriendRelationRepository : Repository<FriendRelation>
{
    public FriendRelationRepository(ChatClientDbContext dbContext) : base(dbContext)
    {
    }
}