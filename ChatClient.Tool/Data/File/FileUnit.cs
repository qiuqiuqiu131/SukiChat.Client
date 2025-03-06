using File.Protobuf;
using Google.Protobuf;

namespace ChatClient.Tool.Data.File;

public class FileUnit
{
    public string Path { get; set; }
    public string FileName { get; set; }
    public string Type { get; set; }
    public int TotleSize { get; set; }
    public int TotleCount { get; set; }
    public int CurrentIndex { get; set; }
    public string Time { get; set; }
    public bool IsFinished { get; set; }
    public Dictionary<int, ByteString> Packs { get; set; } = new();

    private byte[]? _content;


    public FileUnit(string path, string fileName, string type)
    {
        Path = path;
        FileName = fileName;
        Type = type;
        Time = DateTime.Now.ToString("yyyyMMddHHmmss");
    }

    public FileUnit(FileHeader fileHeader)
    {
        Path = fileHeader.Path;
        FileName = fileHeader.FileName;
        Type = fileHeader.Type;
        TotleSize = fileHeader.TotleSize;
        TotleCount = fileHeader.TotleCount;
        Time = fileHeader.Time;
    }

    public bool AddPack(int index, ByteString data)
    {
        if (Packs.ContainsKey(index)) return false;
        Packs.Add(index, data);
        if (index == TotleCount)
            IsFinished = true;
        return true;
    }

    /// <summary>
    /// 合并操作只能执行一次，执行过后便会清空所有文件分片
    /// </summary>
    /// <returns></returns>
    public byte[]? Combine()
    {
        if (_content != null) return _content;
        try
        {
            // 检查文件包是否完整
            if (Packs.Count != TotleCount)
                return null;

            // 检查序号是否连续
            for (int i = 1; i <= TotleCount; i++)
            {
                if (!Packs.ContainsKey(i))
                    return null;
            }

            // 按顺序合并所有包
            using var memoryStream = new MemoryStream();
            for (int i = 1; i <= TotleCount; i++)
            {
                Packs[i].WriteTo(memoryStream);
            }

            _content = memoryStream.ToArray();
            Packs.Clear();
            return _content;
        }
        catch
        {
            return null;
        }
    }

    public void Clear()
    {
        Packs.Clear();
    }
}