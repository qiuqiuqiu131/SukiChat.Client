using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.DataBase.Repository;

public class GroupRequestRepository : Repository<GroupRequest>
{
    public GroupRequestRepository(ChatClientDbContext dbContext) : base(dbContext)
    {
    }
}