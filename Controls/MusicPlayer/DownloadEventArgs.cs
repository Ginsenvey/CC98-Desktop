// DownloadEventArgs.cs

using System;

namespace CC98.Controls.MusicPlayer;

public class DownloadEventArgs : EventArgs
{
    public DownloadEventArgs(string source)
    {
        Source = source;
    }

    public string Source { get; }
}