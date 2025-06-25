using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SqlSugar;

namespace ChatClient.DataBase.Data;

[Microsoft.EntityFrameworkCore.Index(nameof(ChatId), IsUnique = true)]
[Table("ChatGroups")]
[SugarTable("ChatGroups")]
[SugarIndex("unique_chatGroup_chatId", nameof(ChatId), OrderByType.Desc, true)]
[SugarIndex("chatGroup_groupId", nameof(GroupId), OrderByType.Asc)]
public class ChatGroup
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
    public string GroupId { get; set; }

    [Required] public string Message { get; set; }

    [Required] public DateTime Time { get; set; }

    [Required] public bool IsRetracted { get; set; }

    [Required]
    [SugarColumn(DefaultValue = "datatime('1900-01-01 00:00:00')")]
    public DateTime RetractedTime { get; set; } = DateTime.MinValue;
}