using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.DataBase.Repository;

public class GroupDeleteRepository(DbContext dbContext)
    : Repository<GroupDelete>(dbContext);