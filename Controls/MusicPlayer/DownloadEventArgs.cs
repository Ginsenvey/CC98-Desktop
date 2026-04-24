// DownloadEventArgs.cs
using System;

namespace CC98.Share.Controls.Primitives;

public class DownloadEventArgs : EventArgs
{
    public string Source { get; }

    public DownloadEventArgs(string source)
    {
        Source = source;
    }
}