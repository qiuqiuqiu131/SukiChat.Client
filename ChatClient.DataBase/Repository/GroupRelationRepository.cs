using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.DataBase.Repository;

public class GroupRelationRepository : Repository<GroupRelation>
{
    public GroupRelationRepository(ChatClientDbContext dbContext) : base(dbContext)
    {
    }
}