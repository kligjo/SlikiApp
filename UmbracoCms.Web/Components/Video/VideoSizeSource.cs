namespace UmbracoCms.Web.Components;

public class VideoSizeSource
{
    public VideoSizeSource(int size, string url)
    {
        Size = size;
        Source = [new() { Url = url }];
    }
    public int Size { get; set; }
    public List<VideoSource> Source { get; set; }
}
