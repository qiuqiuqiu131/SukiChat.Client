using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;

namespace ChatClient.DataBase.Repository;

public class FriendDeleteRepository(ChatClientDbContext dbContext)
    : Repository<FriendDelete>(dbContext);