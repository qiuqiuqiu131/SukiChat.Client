using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatClient.DataBase.EfCore.Data;

[Table("Groups")]
public class Group
{
    [Key] [StringLength(10)] public string Id { get; set; }

    [StringLength(30)] public string Name { get; set; }

    [StringLength(100)] public string? Description { get; set; }

    public DateTime CreateTime { get; set; }

    public int HeadIndex { get; set; } = 1;

    public bool IsCustomHead { get; set; }

    public bool IsDisband { get; set; }
}