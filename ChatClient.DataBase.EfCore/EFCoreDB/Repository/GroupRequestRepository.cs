using ChatClient.DataBase.EfCore.Data;
using ChatClient.DataBase.EfCore.EFCoreDB.UnitOfWork;

namespace ChatClient.DataBase.EfCore.EFCoreDB.Repository;

public class GroupRequestRepository : Repository<GroupRequest>
{
    public GroupRequestRepository(ChatClientDbContext dbContext) : base(dbContext)
    {
    }
}