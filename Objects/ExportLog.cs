using System;
using System.Collections.Generic;
using static CC98.Services.AppLog;

namespace CC98.Objects;

public class ExportLog
{
    public string AppName { get; set; } = "";
    public DateTime ExportTime { get; set; }
    public int LogCount { get; set; }
    public List<LogEntry> Logs { get; set; } = [];
}