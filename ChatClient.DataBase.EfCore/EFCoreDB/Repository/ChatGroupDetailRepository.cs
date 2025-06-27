using ChatClient.DataBase.EfCore.Data;
using ChatClient.DataBase.EfCore.EFCoreDB.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.DataBase.EfCore.EFCoreDB.Repository;

public class ChatGroupDetailRepository(DbContext dbContext)
    : Repository<ChatGroupDetail>(dbContext);