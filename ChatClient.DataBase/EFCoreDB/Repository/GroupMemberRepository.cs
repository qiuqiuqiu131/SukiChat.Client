using ChatClient.DataBase.Data;
using ChatClient.DataBase.EFCoreDB.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.DataBase.EFCoreDB.Repository;

public class GroupMemberRepository(DbContext dbContext)
    : Repository<GroupMember>(dbContext);