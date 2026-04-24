using CC98.Share.Controls.Primitives;
using System;

namespace UbbRender.Common;

//媒体事件统一参数
public class MediaClickEventArgs : EventArgs
{
    public string Source { get; }
    public MediaType MediaType { get; }
    public MediaClickEventArgs(string source, MediaType mediaType)
    {
        Source = source;
        MediaType = mediaType;
    }
        
}