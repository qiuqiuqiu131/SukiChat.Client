namespace ChatClient.Tool.Media.Video;

public struct VideoCaptureDeviceInfo
{
    public string ID;
    public string Name;
}

public class VideoResolution
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int Fps { get; set; }

    public override string ToString()
    {
        return $"{Width}x{Height} @ {Fps}fps";
    }
}