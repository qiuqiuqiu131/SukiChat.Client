using SqlSugar;

namespace ChatClient.DataBase.SqlSugar.Data;

[SugarTable("Groups")]
public class Group
{
    [SugarColumn(IsPrimaryKey = true, Length = 10)]
    public string Id { get; set; }
    
    [SugarColumn(Length = 30)]
    public string Name { get; set; }
    
    [SugarColumn(IsNullable = true, Length = 100)]
    public string? Description { get; set; }

    public DateTime CreateTime { get; set; }

    [SugarColumn(DefaultValue = "1")] public int HeadIndex { get; set; } = 1;

    public bool IsCustomHead { get; set; }

    public bool IsDisband { get; set; }
}