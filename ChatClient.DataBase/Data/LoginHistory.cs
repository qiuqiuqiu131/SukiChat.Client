using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SqlSugar;

namespace ChatClient.DataBase.Data;

[Table("LoginHistorys")]
[SugarTable("LoginHistorys")]
public class LoginHistory
{
    [Key]
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public string Id { get; set; }

    [Required] public string Password { get; set; }

    [Required] public DateTime LastLoginTime { get; set; }
}