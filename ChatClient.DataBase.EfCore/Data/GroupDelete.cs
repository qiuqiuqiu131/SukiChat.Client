using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatClient.DataBase.EfCore.Data;

[Table("GroupDeletes")]
public class GroupDelete
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int DeleteId { get; set; }

    public string GroupId { get; set; }

    public string MemberId { get; set; }

    public int DeleteMethod { get; set; }

    public string OperateUserId { get; set; }

    public DateTime DeleteTime { get; set; }
}