namespace ChatClient.Tool.Data.File;

public class FileDto
{
    public string FileName { get; set; }
    public string FilePath { get; set; }
    public string Type { get; set; }
    public byte[] Content { get; set; }
}