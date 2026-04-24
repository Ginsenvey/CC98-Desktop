using System;
using CC98.Controls.Primitives;

namespace CC98.Controls.UbbTextBlock.Common.Events;

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