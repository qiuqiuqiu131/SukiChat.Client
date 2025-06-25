using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SqlSugar;

namespace ChatClient.DataBase.Data;

[Table("Groups")]
[SugarTable("Groups")]
public class Group
{
    [Key]
    [StringLength(10)]
    [SugarColumn(IsPrimaryKey = true, Length = 10)]
    public string Id { get; set; }

    [StringLength(30)]
    [SugarColumn(Length = 30)]
    public string Name { get; set; }

    [StringLength(100)]
    [SugarColumn(IsNullable = true, Length = 100)]
    public string? Description { get; set; }

    public DateTime CreateTime { get; set; }

    [SugarColumn(DefaultValue = "1")] public int HeadIndex { get; set; } = 1;

    public bool IsCustomHead { get; set; }

    public bool IsDisband { get; set; }
}