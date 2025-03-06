using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.DataBase.Repository;

public class GroupRepository : Repository<Group>
{
    public GroupRepository(ChatClientDbContext dbContext) : base(dbContext)
    {
    }
}