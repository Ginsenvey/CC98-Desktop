using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace CC98.Services;

/// <summary>
/// 用于缓存大型json数据，标记数据读取状态。
/// </summary>
public class LocalCache
{
    private readonly string _content;
    private readonly string _path;
    private readonly bool _isAvailable;
    private readonly string _message;
    //公开属性
    public string Content => _isAvailable ? _content : throw new InvalidOperationException($"读取位于{_path}的文本出错：{_message}");
    public string CachePath => _path;
    public bool IsAvailable => _isAvailable;
    public string Message => _message;

    // 私有构造函数，仅内部使用
    private LocalCache(string path, string content, bool isAvailable, string message)
    {
        _path = path;
        _content = content;
        _isAvailable = isAvailable;
        _message = message;
    }

    public static async Task<LocalCache> CreateAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("路径不能为空或空白", nameof(path));

        try
        {
            if (!File.Exists(path))
                return new LocalCache(path, string.Empty, false, "文件不存在");

            string content = await File.ReadAllTextAsync(path, cancellationToken);

            if (string.IsNullOrEmpty(content))
                return new LocalCache(path, string.Empty, true, "文件内容为空");

            return new LocalCache(path, content, true, "读取成功");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new LocalCache(path, string.Empty, false, $"读取失败: {ex.Message}");
        }
    }

    public static LocalCache Create(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("路径不能为空或空白", nameof(path));

        try
        {
            if (!File.Exists(path))
                return new LocalCache(path, string.Empty, false, "文件不存在");

            string content = File.ReadAllText(path);

            if (string.IsNullOrEmpty(content))
                return new LocalCache(path, string.Empty, true, "文件内容为空");

            return new LocalCache(path, content, true, "读取成功");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new LocalCache(path, string.Empty, false, $"读取失败: {ex.Message}");
        }
    }

    public T ReadAs<T>()
    {
        if (!_isAvailable)
            throw new InvalidOperationException($"缓存不可用: {_message}");

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<T>(_content, options)!;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"反序列化失败: {ex.Message}", ex);
        }
    }


    public static async Task<(bool Success, string Message)> SaveAsync<T>(
        string path,
        T data,
        JsonSerializerContext? context = null,
        bool createDirectory = true)
    {
        if (string.IsNullOrWhiteSpace(path))
            return (false, "路径不能为空或空白");

        if (data == null)
            return (false, "要保存的数据不能为null");

        try
        {
            // 1. 确保目录存在
            if (createDirectory)
            {
                var directory = System.IO.Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
            }

            // 2. 序列化为JSON
            string jsonContent;
            if (context != null)
            {
                // AOT兼容模式：使用源生成器
                jsonContent = JsonSerializer.Serialize(data, typeof(T), context);
            }
            else
            {
                // 反射模式（非AOT环境）
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true, // 美化输出，便于阅读
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                jsonContent = JsonSerializer.Serialize(data, options);
            }

            // 3. 写入文件（原子操作，避免写入过程中文件损坏）
            await WriteFileSafelyAsync(path, jsonContent);

            return (true, "保存成功");
        }
        catch (JsonException ex)
        {
            return (false, $"序列化失败: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return (false, $"没有写入权限: {ex.Message}");
        }
        catch (IOException ex)
        {
            return (false, $"IO错误: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"保存过程中发生未知错误: {ex.Message}");
        }
    }

    private static async Task WriteFileSafelyAsync(string path, string content)
    {
        var directory = Path.GetDirectoryName(path);

        // 确保目录存在
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        try
        {
            // 直接写入，使用重试机制
            const int maxRetries = 3;
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    await File.WriteAllTextAsync(path, content, Encoding.UTF8);
                    return; // 成功写入，退出
                }
                catch (IOException) when (i < maxRetries - 1)
                {
                    // 如果是IO错误，等待后重试
                    await Task.Delay(100 * (i + 1));
                }
            }
        }
        catch (Exception ex)
        {
            // 记录错误
            Console.WriteLine($"写入文件失败: {ex.Message}");
            throw;
        }
    }


    public static (bool Success, string Message) Save<T>(
    string path,
    T data,
    JsonSerializerContext? context = null,
    bool createDirectory = true)
    {
        if (string.IsNullOrWhiteSpace(path))
            return (false, "路径不能为空或空白");

        if (data == null)
            return (false, "要保存的数据不能为null");

        try
        {
            // 1. 确保目录存在
            if (createDirectory)
            {
                var directory = System.IO.Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
            }

            // 2. 序列化为JSON
            string jsonContent;
            if (context != null)
            {
                jsonContent = JsonSerializer.Serialize(data, typeof(T), context);
            }
            else
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };
                jsonContent = JsonSerializer.Serialize(data, options);
            }

            // 3. 原子性写入
            WriteFileAtomically(path, jsonContent);

            return (true, "保存成功");
        }
        catch (Exception ex) when (ex is JsonException or UnauthorizedAccessException or IOException)
        {
            return (false, $"保存失败: {ex.Message}");
        }
    }

    private static void WriteFileAtomically(string path, string content)
    {
        var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";

        try
        {
            File.WriteAllText(tempPath, content, System.Text.Encoding.UTF8);

            if (File.Exists(path))
                File.Replace(tempPath, path, $"{path}.bak");
            else
                File.Move(tempPath, path);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    /// <summary>
    /// 直接保存JSON字符串到文件（不进行序列化）
    /// </summary>
    public static async Task<(bool Success, string Message)> SaveJsonAsync(
        string path,
        string jsonContent,
        bool createDirectory = true,
        bool validateJson = false)  // 可选：验证JSON格式
    {
        if (string.IsNullOrWhiteSpace(path))
            return (false, "路径不能为空或空白");

        if (string.IsNullOrWhiteSpace(jsonContent))
            return (false, "JSON内容不能为空");

        try
        {
            // 可选：验证JSON格式
            if (validateJson)
            {
                try
                {
                    using var doc = JsonDocument.Parse(jsonContent);
                }
                catch (JsonException ex)
                {
                    return (false, $"无效的JSON格式: {ex.Message}");
                }
            }

            // 确保目录存在
            if (createDirectory)
            {
                var directory = System.IO.Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
            }

            // 原子性写入
            await WriteFileSafelyAsync(path, jsonContent);

            return (true, "保存成功");
        }
        catch (UnauthorizedAccessException ex)
        {
            return (false, $"没有写入权限: {ex.Message}");
        }
        catch (IOException ex)
        {
            return (false, $"IO错误: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"保存过程中发生错误: {ex.Message}");
        }
    }
    /// <summary>
    /// 同步版本
    /// </summary>
    public static (bool Success, string Message) SaveJson(
        string path,
        string jsonContent,
        bool createDirectory = true,
        bool validateJson = false)
    {
        if (string.IsNullOrWhiteSpace(path))
            return (false, "路径不能为空或空白");

        if (string.IsNullOrWhiteSpace(jsonContent))
            return (false, "JSON内容不能为空");

        try
        {
            // 可选：验证JSON格式
            if (validateJson)
            {
                try
                {
                    using var doc = JsonDocument.Parse(jsonContent);
                }
                catch (JsonException ex)
                {
                    return (false, $"无效的JSON格式: {ex.Message}");
                }
            }

            // 确保目录存在
            if (createDirectory)
            {
                var directory = System.IO.Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
            }
            // 原子性写入
            WriteFileAtomically(path, jsonContent);
            return (true, "保存成功");
        }
        catch (UnauthorizedAccessException ex)
        {
            return (false, $"没有写入权限: {ex.Message}");
        }
        catch (IOException ex)
        {
            return (false, $"IO错误: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"保存过程中发生错误: {ex.Message}");
        }
    }
}
