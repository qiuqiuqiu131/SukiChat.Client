using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatClient.DataBase.Data;

[Microsoft.EntityFrameworkCore.Index(nameof(ChatId), IsUnique = true)]
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
    
    [Required] public bool IsRetracted { get; set; }

    [Required] public DateTime RetractedTime { get; set; } = DateTime.MinValue;
}