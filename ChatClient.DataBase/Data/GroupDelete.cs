using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SqlSugar;

namespace ChatClient.DataBase.Data;

[Table("GroupDeletes")]
[SugarTable("GroupDeletes")]
[SugarIndex("unique_groupDelete_deleteId", nameof(DeleteId), OrderByType.Desc, true)]
public class GroupDelete
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    public int DeleteId { get; set; }

    public string GroupId { get; set; }

    public string MemberId { get; set; }

    public int DeleteMethod { get; set; }

    public string OperateUserId { get; set; }

    public DateTime DeleteTime { get; set; }
}