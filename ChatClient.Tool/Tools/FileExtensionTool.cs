namespace ChatClient.Tool.Tools;

public static class FileExtensionTool
{
    private static HashSet<string> ImageDic = new() { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff" };

    public static bool IsImage(string path)
    {
        string extens = Path.GetExtension(path);
        return ImageDic.Contains(extens.ToLower());
    }
}