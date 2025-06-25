using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SqlSugar;

namespace ChatClient.DataBase.Data;

[Table("FriendDeletes")]
[SugarTable("FriendDeletes")]
[SugarIndex("unique_friendDelete_deleteId", nameof(DeleteId), OrderByType.Desc, true)]
public class FriendDelete
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    public int DeleteId { get; set; }

    public string UseId1 { get; set; }

    public string UserId2 { get; set; }

    public DateTime DeleteTime { get; set; }
}