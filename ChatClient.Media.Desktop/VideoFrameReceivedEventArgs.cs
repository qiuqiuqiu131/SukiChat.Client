using SIPSorceryMedia.Abstractions;

namespace ChatClient.Media.Desktop;

public class VideoFrameReceivedEventArgs : EventArgs
{
    public bool IsCamera { get; set; }
    public byte[]? FrameData { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public VideoPixelFormatsEnum PixelFormat { get; set; }
}