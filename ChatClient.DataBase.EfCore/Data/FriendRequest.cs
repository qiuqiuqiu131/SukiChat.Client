using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatClient.DataBase.EfCore.Data;

[Table("FriendRequests")]
public class FriendRequest
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int RequestId { get; set; }

    [StringLength(10)] public string UserFromId { get; set; }

    [StringLength(10)] public string UserTargetId { get; set; }

    public string Message { get; set; }

    public string Group { get; set; }

    public string Remark { get; set; }

    public DateTime RequestTime { get; set; }

    public bool IsAccept { get; set; }

    public bool IsSolved { get; set; }

    public DateTime? SolveTime { get; set; }
}