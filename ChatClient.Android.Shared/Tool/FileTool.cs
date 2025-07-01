#if ANDROID
using Application = Android.App.Application;

namespace ChatClient.Android.Shared.Tool;

internal static class FileTool
{
    public static void CopyAssetsToFilesIfNeeded(string[] assetFiles)
    {
        var assets = Application.Context.Assets;
        var filesDir = Application.Context.FilesDir.AbsolutePath;

        foreach (var assetFile in assetFiles)
        {
            string destPath = Path.Combine(filesDir, assetFile);

            // 如果文件不存在，则从Assets复制
            if (!System.IO.File.Exists(destPath))
            {
                string configDir = Path.GetDirectoryName(destPath);
                if (!Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }

                using (var assetStream = assets.Open(assetFile))
                using (var fileStream = System.IO.File.Create(destPath))
                {
                    assetStream.CopyTo(fileStream);
                }
            }
        }
    }
}
#endif