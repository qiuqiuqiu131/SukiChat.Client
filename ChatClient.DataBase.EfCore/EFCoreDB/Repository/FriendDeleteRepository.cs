using ChatClient.DataBase.EfCore.Data;
using ChatClient.DataBase.EfCore.EFCoreDB.UnitOfWork;

namespace ChatClient.DataBase.EfCore.EFCoreDB.Repository;

public class FriendDeleteRepository(ChatClientDbContext dbContext)
    : Repository<FriendDelete>(dbContext);