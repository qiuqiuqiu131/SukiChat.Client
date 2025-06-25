using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SqlSugar;

namespace ChatClient.DataBase.Data;

[Microsoft.EntityFrameworkCore.Index(nameof(ChatId), IsUnique = true)]
[Table("ChatPrivates")]
[SugarTable("ChatPrivates")]
[SugarIndex("unique_chatPrivate_chatId", nameof(ChatId), OrderByType.Desc, true)]
[SugarIndex("chatPrivate_userFromId_userTargetId", nameof(UserFromId), OrderByType.Asc,
    nameof(UserTargetId), OrderByType.Asc)]
public class ChatPrivate
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [Required] public int ChatId { get; set; }

    [Required]
    [StringLength(10)]
    [SugarColumn(Length = 10)]
    public string UserFromId { get; set; }

    [Required]
    [StringLength(10)]
    [SugarColumn(Length = 10)]
    public string UserTargetId { get; set; }

    [Required] public string Message { get; set; }

    [Required] public DateTime Time { get; set; }

    [Required] public bool IsRetracted { get; set; }

    [Required]
    [SugarColumn(DefaultValue = "datatime('1900-01-01 00:00:00')")]
    public DateTime RetractedTime { get; set; } = DateTime.MinValue;
}