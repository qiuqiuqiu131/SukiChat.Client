using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatClient.DataBase.EfCore.Data;

[Table("GroupReceiveds")]
public class GroupReceived
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required] public int RequestId { get; set; }

    [StringLength(10)] public string UserFromId { get; set; }

    [StringLength(10)] public string GroupId { get; set; }

    public DateTime ReceiveTime { get; set; }

    public string Message { get; set; }

    public bool IsAccept { get; set; }

    public bool IsSolved { get; set; }

    public DateTime? SolveTime { get; set; }

    [StringLength(10)] public string? AcceptByUserId { get; set; }
}