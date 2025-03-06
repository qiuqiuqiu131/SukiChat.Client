using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatClient.DataBase.Data;

public class GroupRequest
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required] public int RequestId { get; set; }

    [StringLength(10)] public string UserFromId { get; set; }

    [StringLength(10)] public string GroupId { get; set; }

    public DateTime RequestTime { get; set; }

    public bool IsAccept { get; set; } = false;

    public bool IsSolved { get; set; } = false;

    public DateTime? SolveTime { get; set; }

    [StringLength(10)] public string? AcceptByUserId { get; set; }
}