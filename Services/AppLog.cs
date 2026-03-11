using CC98.Services;
using CC98.Services.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Storage;
namespace CC98.Services;
public class AppLog
{
    private readonly List<LogEntry> _logs = new();
    private string _logDirectory;
    private readonly string _appName;
    private readonly object _lock = new();
    private bool _isInitialized = false;

    // 日志条目结构
    public record LogEntry(
        string Domain,
        string Info,
        string Message,
        DateTime Time
    );

    /// <summary>
    /// 创建AppLog实例
    /// </summary>
    /// <param name="appName">应用名称</param>
    /// <param name="logDirectory">日志目录（为空则使用默认目录）</param>
    public AppLog(string appName, string? logDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(appName))
            throw new ArgumentException("应用名称不能为空", nameof(appName));

        _appName = SanitizeFileName(appName);

        // 设置日志目录
        _logDirectory = string.IsNullOrWhiteSpace(logDirectory)
            ? GetDefaultLogDirectory()
            : logDirectory;
    }

    #region 核心日志记录方法

    /// <summary>
    /// 写入日志（异步）。禁止使用同步方法读写文件。
    /// </summary>
    /// <param name="domain">事件发生域</param>
    /// <param name="info">事件简述</param>
    /// <param name="message">事件详情</param>
    public async Task WriteAsync(
        string domain,
        string info,
        string message="")
    {
        await InitializeAsyncIfNeeded();

        var entry = new LogEntry(
            Domain: domain?.Trim() ?? "Unknown",
            Info: info?.Trim() ?? string.Empty,
            Message: message?.Trim() ?? string.Empty,
            Time: GetBeijingTime()
        );

        lock (_lock)
        {
            _logs.Add(entry);
        }

        // 控制台输出
        WriteToConsole(entry);

        // 自动保存（每次写入都保存，确保不丢失）
        await SaveToFileAsync();
    }


    /// <summary>
    /// 批量写入日志
    /// </summary>
    public async Task WriteBatchAsync(IEnumerable<(string Domain, string Info, string Message)> entries)
    {
        await InitializeAsyncIfNeeded();

        var beijingTime = GetBeijingTime();
        var newEntries = new List<LogEntry>();

        foreach (var entry in entries)
        {
            var logEntry = new LogEntry(
                Domain: entry.Domain?.Trim() ?? "Unknown",
                Info: entry.Info?.Trim() ?? string.Empty,
                Message: entry.Message?.Trim() ?? string.Empty,
                Time: beijingTime
            );

            newEntries.Add(logEntry);
            WriteToConsole(logEntry);
        }

        lock (_lock)
        {
            _logs.AddRange(newEntries);
        }

        await SaveToFileAsync();
    }

    #endregion

    #region 文件存储功能

    /// <summary>
    /// 保存日志到默认位置
    /// </summary>
    public async Task SaveToFileAsync(string? customName = null)
    {
        await InitializeAsyncIfNeeded();

        try
        {
            string fileName = customName ?? GetDefaultLogFileName();
            string filePath = Path.Combine(_logDirectory, fileName);

            var logsToSave = GetRecentLogs(maxCount: null); // 保存所有日志

            string json = JsonSerialize.Serialize(logsToSave);

            await LocalCache.SaveJsonAsync(filePath, json, validateJson: false);

        }
        catch (Exception ex)
        {
            // 保存失败时，尝试记录到控制台
            Console.WriteLine($"[错误] 保存日志失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 另存日志到用户桌面
    /// </summary>
    public async Task<(bool Success, string Path)> SaveToDesktopAsync(
        string? fileName = null,
        int? maxLogs = 1000)
    {
        await InitializeAsyncIfNeeded();

        try
        {
            string desktopPath = Environment.GetFolderPath(
                Environment.SpecialFolder.Desktop);

            string targetFileName = fileName ?? $"{_appName}_日志_{DateTime.Now:yyyy年MM月dd日_HH时mm分}.json";
            string targetPath = Path.Combine(desktopPath, targetFileName);

            // 确定要保存的日志
            var logsToSave = GetRecentLogs(maxLogs);

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new BeijingTimeConverter() }
            };

            var exportData = new
            {
                AppName = _appName,
                ExportTime = GetBeijingTime(),
                LogCount = logsToSave.Count,
                Logs = logsToSave
            };

            string json = JsonSerialize.Serialize(exportData);

            var result = await LocalCache.SaveJsonAsync(
                targetPath, json, validateJson: false);

            if (result.Success)
            {
                await WriteAsync("AppLog", "日志导出", $"日志已导出到桌面: {targetPath}");
                return (true, targetPath);
            }

            return (false, result.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[错误] 导出日志到桌面失败: {ex.Message}");
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// 保存为纯文本格式（便于阅读）
    /// </summary>
    public async Task<string> SaveAsTextAsync(string? filePath = null)
    {
        await InitializeAsyncIfNeeded();

        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                string desktopPath = Environment.GetFolderPath(
                    Environment.SpecialFolder.Desktop);
                filePath = Path.Combine(desktopPath,
                    $"{_appName}_日志_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            }

            var recentLogs = GetRecentLogs(maxCount: null);

            using var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);

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
                    if (!string.IsNullOrWhiteSpace(log.Message))
                    {
                        writer.WriteLine($"    详情: {log.Message}");
                    }
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
    /// 清理旧日志文件
    /// </summary>
    public async Task CleanOldFilesAsync(int daysToKeep = 30)
    {
        await InitializeAsyncIfNeeded();

        try
        {
            var logFiles = Directory.GetFiles(_logDirectory, "*.json")
                .Select(f => new FileInfo(f))
                .Where(f => f.Name.StartsWith($"{_appName}_log_"))
                .ToList();

            int deletedCount = 0;
            DateTime cutoffDate = DateTime.Now.AddDays(-daysToKeep);

            foreach (var file in logFiles)
            {
                if (file.LastWriteTime < cutoffDate)
                {
                    file.Delete();
                    deletedCount++;
                }
            }

            if (deletedCount > 0)
            {
                await WriteAsync("AppLog", "清理旧文件",
                    $"清理了 {deletedCount} 个 {daysToKeep} 天前的日志文件");
            }
        }
        catch (Exception ex)
        {
            await WriteAsync("AppLog", "清理失败", $"清理旧日志文件失败: {ex.Message}");
        }
    }

    #endregion

    #region 查询与读取功能

    /// <summary>
    /// 获取日志（可指定数量）
    /// </summary>
    public List<LogEntry> GetRecentLogs(int? maxCount = null)
    {
        lock (_lock)
        {
            var query = _logs.OrderBy(l => l.Time).AsEnumerable();

            if (maxCount.HasValue)
                query = query.TakeLast(maxCount.Value);

            return query.ToList();
        }
    }

    /// <summary>
    /// 按域名筛选日志
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

            return query.ToList();
        }
    }

    /// <summary>
    /// 按时间范围查询日志
    /// </summary>
    public List<LogEntry> GetLogsByTimeRange(DateTime from, DateTime to)
    {
        lock (_lock)
        {
            return _logs
                .Where(l => l.Time >= from && l.Time <= to)
                .OrderBy(l => l.Time)
                .ToList();
        }
    }

    /// <summary>
    /// 搜索包含关键词的日志
    /// </summary>
    public List<LogEntry> SearchLogs(string keyword, bool searchInMessageOnly = false)
    {
        lock (_lock)
        {
            return _logs
                .Where(l =>
                    (!searchInMessageOnly &&
                     (l.Domain.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                      l.Info.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                      l.Message.Contains(keyword, StringComparison.OrdinalIgnoreCase))) ||
                    (searchInMessageOnly &&
                     l.Message.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(l => l.Time)
                .ToList();
        }
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 初始化日志系统
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            // 创建日志目录
            if (!Directory.Exists(_logDirectory))
                Directory.CreateDirectory(_logDirectory);

            // 尝试加载最近的日志文件
            await LoadRecentLogsAsync();

            _isInitialized = true;

            // 记录初始化完成
            await WriteAsync("AppLog", "系统初始化", "日志系统初始化完成");
        }
        catch (Exception ex)
        {
            // 使用备用目录
            _logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"{_appName}_日志");

            Directory.CreateDirectory(_logDirectory);
            _isInitialized = true;

            await WriteAsync("AppLog", "初始化备用方案",
                $"日志系统使用备用目录: {ex.Message}");
        }
    }

    private async Task InitializeAsyncIfNeeded()
    {
        if (!_isInitialized)
            await InitializeAsync();
    }

    private string GetDefaultLogDirectory()
    {
        return ApplicationData.Current.LocalCacheFolder.Path;
    }

    private string GetDefaultLogFileName()
    {
        return $"{_appName}_log_{DateTime.Now:yyyyMMdd}.json";
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return new string(fileName
            .Where(ch => !invalidChars.Contains(ch))
            .ToArray());
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
    /// 控制台输出
    /// </summary>
    private static void WriteToConsole(LogEntry entry)
    {
        Console.WriteLine($"[{entry.Time:HH:mm:ss}] [{entry.Domain}] {entry.Info}");
        if (!string.IsNullOrWhiteSpace(entry.Message))
        {
            Console.WriteLine($"      {entry.Message}");
        }
    }

    /// <summary>
    /// 从文件加载最近日志
    /// </summary>
    private async Task LoadRecentLogsAsync()
    {
        try
        {
            // 查找最新的日志文件
            var logFiles = Directory.GetFiles(_logDirectory, "*.json")
                .Where(f => Path.GetFileName(f).StartsWith($"{_appName}_log_"))
                .OrderByDescending(f => f)
                .FirstOrDefault();

            if (logFiles != null)
            {
                var cache = await LocalCache.CreateAsync(logFiles);
                if (cache.IsAvailable)
                {
                    var loadedLogs = cache.ReadAs<List<LogEntry>>();
                    lock (_lock)
                    {
                        _logs.AddRange(loadedLogs);
                    }
                }
            }
        }
        catch
        {
            // 加载失败不影响正常使用
        }
    }

    #endregion

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

    #region 属性访问器

    public int LogCount
    {
        get
        {
            lock (_lock) return _logs.Count;
        }
    }

    public string LogDirectory => _logDirectory;

    public bool IsInitialized => _isInitialized;

    public List<string> Domains
    {
        get
        {
            lock (_lock)
            {
                return _logs
                    .Select(l => l.Domain)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToList();
            }
        }
    }

    #endregion
}