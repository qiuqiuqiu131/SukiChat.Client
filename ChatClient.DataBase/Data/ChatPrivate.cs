using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatClient.DataBase.Data;

public class ChatPrivate
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required] public int ChatId { get; set; }

    [Required] [StringLength(10)] public string UserFromId { get; set; }

    [Required] [StringLength(10)] public string UserTargetId { get; set; }

    [Required] public string Message { get; set; }

    [Required] public DateTime Time { get; set; }

    [Required] public bool IsReaded { get; set; } = false;
}