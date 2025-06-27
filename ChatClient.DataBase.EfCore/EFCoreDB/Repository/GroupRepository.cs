using ChatClient.DataBase.EfCore.Data;
using ChatClient.DataBase.EfCore.EFCoreDB.UnitOfWork;

namespace ChatClient.DataBase.EfCore.EFCoreDB.Repository;

public class GroupRepository : Repository<Group>
{
    public GroupRepository(ChatClientDbContext dbContext) : base(dbContext)
    {
    }
}