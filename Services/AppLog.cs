using CC98.Objects;
using CC98.Services.Extensions;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Windows.Storage;

namespace CC98.Services;

public class AppLog
{
    private readonly string _appName;
    private readonly Lock _lock = new();
    private readonly List<LogEntry> _logs = [];

    /// <summary>
    ///     创建AppLog实例
    /// </summary>
    /// <param name="appName">应用名称</param>
    /// <param name="logDirectory">日志目录（为空则使用默认目录）</param>
    public AppLog(string appName, string? logDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(appName))
        {
            throw new ArgumentException("应用名称不能为空", nameof(appName));
        }
        _appName = SanitizeFileName(appName);
        LogDirectory = string.IsNullOrWhiteSpace(logDirectory)? DefaultLogDirectory: logDirectory;
    }

    // 日志条目结构
    public record LogEntry(
        string Domain,
        string Info,
        string Message,
        DateTime Time
    );

    #region JSON 转换器（确保时间正确序列化）

    public class BeijingTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetDateTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy-MM-dd HH:mm:ss"));
        }
    }

    #endregion

    #region 核心日志记录方法

    /// <summary>
    /// 写入日志。禁止使用同步方法读写文件。
    /// </summary>
    /// <param name="domain">事件发生域</param>
    /// <param name="info">事件简述</param>
    /// <param name="message">事件详情</param>
    public async Task WriteAsync(string domain,string info,string message = "")
    {
        await InitializeAsyncIfNeeded();

        var entry = new LogEntry(
            domain?.Trim() ?? "未知的错误发生域",
            info?.Trim() ?? string.Empty,
            message?.Trim() ?? string.Empty,
            GetBeijingTime()
        );

        lock (_lock)
        {
            _logs.Add(entry);
        }

        // 自动保存
        await SaveToFileAsync();
    }


    #endregion

    #region 文件存储功能

    /// <summary>
    ///     保存日志到默认位置
    /// </summary>
    public async Task SaveToFileAsync(string? customName = null, CancellationToken cancellationToken = default)
    {
        await InitializeAsyncIfNeeded();

        try
        {
            var fileName = customName ?? GetDefaultLogFileName();
            var filePath = Path.Combine(LogDirectory, fileName);
            var logsToSave = GetRecentLogs(null); // 保存所有日志
            var cache = new JsonFileCache<List<LogEntry>>(filePath);
            await cache.UpdateDataAsync(logsToSave, cancellationToken);
        }
        catch (Exception ex)
        {
            //调试输出
            Debug.WriteLine($"[错误] 保存日志失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 另存日志到用户桌面
    /// </summary>
    public async Task<(bool Success, string Path)> SaveToDesktopAsync(string? fileName = null,int? maxLogs = 1000,CancellationToken cancellationToken=default)
    {
        await InitializeAsyncIfNeeded();

        try
        {
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var targetFileName = fileName ?? $"{_appName}_日志_{DateTime.Now:yyyy年MM月dd日_HH时mm分}.json";
            var targetPath = Path.Combine(desktopPath, targetFileName);

            // 确定要保存的日志
            var logsToSave = GetRecentLogs(maxLogs);

            var exportData = new ExportLog
            {
                AppName = _appName,
                ExportTime = GetBeijingTime(),
                LogCount = logsToSave.Count,
                Logs = logsToSave
            };

            var cache = new JsonFileCache<ExportLog>(targetPath);
            await cache.UpdateDataAsync(exportData, cancellationToken);
            return (true, targetPath);
        }
        catch (Exception ex)
        {
            await WriteAsync("AppLog", "导出失败", $"导出日志到桌面失败: {ex.Message}");
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// 保存为纯文本格式，计划增加到启动窗口的日志查看器中，以便进行异常追踪
    /// </summary>
    public async Task<string> SaveAsTextAsync(string? filePath = null)
    {
        await InitializeAsyncIfNeeded();

        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                var desktopPath = Environment.GetFolderPath(
                    Environment.SpecialFolder.Desktop);
                filePath = Path.Combine(desktopPath,
                    $"{_appName}_日志_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            }

            var recentLogs = GetRecentLogs(null);

            await using var writer = new StreamWriter(filePath, false, Encoding.UTF8);

            writer.WriteLine($"=== {_appName} 日志 ===");
            writer.WriteLine($"导出时间: {GetBeijingTime():yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine($"日志数量: {recentLogs.Count}");
            writer.WriteLine(new string('=', 60));
            writer.WriteLine();

            // 按域名分组输出
            var groupedLogs = recentLogs
                .GroupBy(l => l.Domain)
                .OrderBy(g => g.Key);

            foreach (var group in groupedLogs)
            {
                writer.WriteLine($"【{group.Key}】");
                writer.WriteLine(new string('-', 40));

                foreach (var log in group.OrderBy(l => l.Time))
                {
                    writer.WriteLine($"{log.Time:HH:mm:ss} - {log.Info}");
                    if (!string.IsNullOrWhiteSpace(log.Message)) writer.WriteLine($"    详情: {log.Message}");
                }

                writer.WriteLine();
            }

            writer.WriteLine(new string('=', 60));
            writer.WriteLine("=== 日志结束 ===");

            await WriteAsync("AppLog", "文本日志导出", $"日志已导出为文本: {filePath}");
            return filePath;
        }
        catch (Exception ex)
        {
            await WriteAsync("AppLog", "文本导出失败", $"导出文本日志失败: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    ///  清理旧日志文件
    /// </summary>
    public async Task<(bool success, int deletedCount)> CleanOldFilesAsync(int daysToKeep = 30)
    {
        await InitializeAsyncIfNeeded();

        try
        {
            var logFiles = Directory.GetFiles(LogDirectory, "*.json")
                .Select(f => new FileInfo(f))
                .Where(f => f.Name.StartsWith($"{_appName}_log_"))
                .ToList();

            var deletedCount = 0;
            var cutoffDate = DateTime.Now.AddDays(-daysToKeep);

            foreach (var file in logFiles)
            {
                if (file.LastWriteTime < cutoffDate)
                {
                    file.Delete();
                    deletedCount++;
                }
            }
            return (true, deletedCount);

        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[错误] 清理旧日志文件失败: {ex.Message}");
            return (false, 0);
        }
    }

    #endregion

    #region 查询与读取功能

    /// <summary>
    ///     获取日志（可指定数量）
    /// </summary>
    public List<LogEntry> GetRecentLogs(int? maxCount = null)
    {
        lock (_lock)
        {
            var query = _logs.OrderBy(l => l.Time).AsEnumerable();

            if (maxCount.HasValue)
                query = query.TakeLast(maxCount.Value);

            return [.. query];
        }
    }

    /// <summary>
    ///     按域名筛选日志
    /// </summary>
    public List<LogEntry> GetLogsByDomain(string domain, int? maxCount = null)
    {
        lock (_lock)
        {
            var query = _logs
                .Where(l => l.Domain.Equals(domain, StringComparison.OrdinalIgnoreCase))
                .OrderBy(l => l.Time);

            if (maxCount.HasValue)
                query = (IOrderedEnumerable<LogEntry>)query.TakeLast(maxCount.Value);

            return [.. query];
        }
    }

    /// <summary>
    ///     按时间范围查询日志
    /// </summary>
    public List<LogEntry> GetLogsByTimeRange(DateTime from, DateTime to)
    {
        lock (_lock)
        {
            return
            [
                .. _logs
                    .Where(l => l.Time >= from && l.Time <= to)
                    .OrderBy(l => l.Time)
            ];
        }
    }

    /// <summary>
    ///     搜索包含关键词的日志
    /// </summary>
    public List<LogEntry> SearchLogs(string keyword, bool searchInMessageOnly = false)
    {
        lock (_lock)
        {
            return
            [
                .. _logs
                    .Where(l =>
                        (!searchInMessageOnly &&
                         (l.Domain.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                          l.Info.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                          l.Message.Contains(keyword, StringComparison.OrdinalIgnoreCase))) ||
                        (searchInMessageOnly &&
                         l.Message.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(l => l.Time)
            ];
        }
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 初始化日志系统
    /// </summary>
    public async Task InitializeAsync()
    {
        if (IsInitialized) return;

        try
        {
            // 创建日志目录
            if (!Directory.Exists(LogDirectory))Directory.CreateDirectory(LogDirectory);

            // 尝试加载最近的日志文件
            await LoadRecentLogsAsync();

            IsInitialized = true;
        }
        catch
        {
            // 使用备用目录
            LogDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                $"{_appName}_日志");

            Directory.CreateDirectory(LogDirectory);
            IsInitialized = true;
        }
    }

    private async Task InitializeAsyncIfNeeded()
    {
        if (!IsInitialized)await InitializeAsync();
    }

    private static string DefaultLogDirectory=>ApplicationData.Current.LocalCacheFolder.Path;
    

    private string GetDefaultLogFileName()
    {
        return $"{_appName}_log_{DateTime.Now:yyyyMMdd}.json";
    }
    //删除非法字符
    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return new([.. fileName.Where(ch => !invalidChars.Contains(ch))]);
    }

    /// <summary>
    /// 获取北京时间
    /// </summary>
    private static DateTime GetBeijingTime()
    {
        try
        {
            // 使用中国标准时间时区
            var beijingTimeZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, beijingTimeZone);
        }
        catch
        {
            // 如果时区不存在，使用UTC+8
            return DateTime.UtcNow.AddHours(8);
        }
    }

    /// <summary>
    ///  从文件加载最近日志
    /// </summary>
    private async Task LoadRecentLogsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 查找最新的日志文件
            var recentLogFilePath =
                (from file in Directory.EnumerateFiles(LogDirectory, "*.json")
                 let fileName = Path.GetFileName(file)
                 where fileName.StartsWith($"{_appName}_log_")
                 orderby file descending
                 select file)
                .FirstOrDefault();

            // 未找到文件不影响使用，直接返回
            if (recentLogFilePath == null) return;

            var cache = new JsonFileCache<LogEntry>(recentLogFilePath);
            var data = await cache.GetDataAsync(cancellationToken);
            if (data == null) return;
            lock (_lock)
            {
                _logs.AddRange(data);
            }
        }
        catch
        {
            Debug.WriteLine("[警告] 加载日志文件失败，可能是文件损坏或格式不正确。将使用空日志继续运行。");
            // 加载失败不影响正常使用
        }
    }

    #endregion

    #region 属性访问器

    public int LogCount
    {
        get
        {
            lock (_lock)
            {
                return _logs.Count;
            }
        }
    }

    public string LogDirectory { get; private set; }

    public bool IsInitialized { get; private set; }

    

    #endregion
}