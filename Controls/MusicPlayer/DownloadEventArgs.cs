// DownloadEventArgs.cs

using System;

namespace CC98.Controls.MusicPlayer;

public class DownloadEventArgs : EventArgs
{
    public string Source { get; }

    public DownloadEventArgs(string source)
    {
        Source = source;
    }
}