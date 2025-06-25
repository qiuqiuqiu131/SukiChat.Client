using ChatClient.DataBase.Data;
using ChatClient.DataBase.EFCoreDB.UnitOfWork;

namespace ChatClient.DataBase.EFCoreDB.Repository;

public class GroupRepository : Repository<Group>
{
    public GroupRepository(ChatClientDbContext dbContext) : base(dbContext)
    {
    }
}