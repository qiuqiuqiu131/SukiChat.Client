using ChatClient.DataBase.Data;
using ChatClient.DataBase.EFCoreDB.UnitOfWork;

namespace ChatClient.DataBase.EFCoreDB.Repository;

public class GroupRelationRepository : Repository<GroupRelation>
{
    public GroupRelationRepository(ChatClientDbContext dbContext) : base(dbContext)
    {
    }
}