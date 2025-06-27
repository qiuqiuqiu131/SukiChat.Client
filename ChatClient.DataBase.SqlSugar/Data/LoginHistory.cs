using SqlSugar;

namespace ChatClient.DataBase.SqlSugar.Data;

[SugarTable("LoginHistorys")]
public class LoginHistory
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public string Id { get; set; }

    public string Password { get; set; }

    public DateTime LastLoginTime { get; set; }
}