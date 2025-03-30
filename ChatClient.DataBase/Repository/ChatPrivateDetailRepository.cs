using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.DataBase.Repository;

public class ChatPrivateDetailRepository(DbContext dbContext)
    : Repository<ChatPrivateDetail>(dbContext);