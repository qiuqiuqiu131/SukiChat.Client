using ChatClient.DataBase.EfCore.Data;
using ChatClient.DataBase.EfCore.EFCoreDB.UnitOfWork;

namespace ChatClient.DataBase.EfCore.EFCoreDB.Repository;

public class GroupRelationRepository : Repository<GroupRelation>
{
    public GroupRelationRepository(ChatClientDbContext dbContext) : base(dbContext)
    {
    }
}