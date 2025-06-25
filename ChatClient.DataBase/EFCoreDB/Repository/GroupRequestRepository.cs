using ChatClient.DataBase.Data;
using ChatClient.DataBase.EFCoreDB.UnitOfWork;

namespace ChatClient.DataBase.EFCoreDB.Repository;

public class GroupRequestRepository : Repository<GroupRequest>
{
    public GroupRequestRepository(ChatClientDbContext dbContext) : base(dbContext)
    {
    }
}