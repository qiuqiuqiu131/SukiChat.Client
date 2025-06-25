using ChatClient.DataBase.Data;
using ChatClient.DataBase.EFCoreDB.UnitOfWork;

namespace ChatClient.DataBase.EFCoreDB.Repository;

public class FriendDeleteRepository(ChatClientDbContext dbContext)
    : Repository<FriendDelete>(dbContext);