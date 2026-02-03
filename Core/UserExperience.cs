using CC98.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace CC98.Kernel.UserExperience;
public static class CustomEmoji
{
    private const string FileName = "custom_emoji.json";
    private static string _filePath;

    // 获取完整文件路径（延迟初始化）
    private static string FilePath
    {
        get
        {
            if (string.IsNullOrEmpty(_filePath))
            {
                // 保持原存储位置：应用本地缓存文件夹
                var folder = Windows.Storage.ApplicationData.Current.LocalCacheFolder;
                _filePath = Path.Combine(folder.Path, FileName);
            }
            return _filePath;
        }
    }

    /// <summary>
    /// 保存新的表情URL
    /// </summary>
    public static async Task SaveEmojiAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("表情URL不能为空", nameof(url));

        try
        {
            // 1. 获取现有列表
            var emojiList = await GetAllEmojiAsync();

            // 2. 检查是否已存在（可选去重）
            if (!emojiList.Contains(url))
            {
                emojiList.Add(url);
            }

            // 3. 使用 LocalCache 保存
            var result = await LocalCache.SaveJsonAsync(FilePath,
                JsonSerializer.Serialize(emojiList),
                createDirectory: false); // 不自动创建目录，因为路径已存在

            if (!result.Success)
            {
                throw new InvalidOperationException($"保存失败: {result.Message}");
            }
        }
        catch (Exception ex)
        {
            // 可以选择记录日志或重新抛出
            throw new InvalidOperationException($"保存表情时发生错误: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 获取所有表情URL列表（异步版本）
    /// </summary>
    public static async Task<List<string>> GetAllEmojiAsync()
    {
        try
        {
            // 使用 LocalCache 读取文件
            var cache = await LocalCache.CreateAsync(FilePath);

            if (cache.IsAvailable && !string.IsNullOrWhiteSpace(cache.Content))
            {
                // 检查是否为有效的JSON（替代原来的 "10:" 检查）
                try
                {
                    var emojiList = JsonSerializer.Deserialize<List<string>>(cache.Content);
                    return emojiList ?? new List<string>();
                }
                catch (JsonException)
                {
                    // JSON格式无效，返回空列表
                    return new List<string>();
                }
            }
        }
        catch (FileNotFoundException)
        {
            // 文件不存在是正常情况，返回空列表
        }
        catch (DirectoryNotFoundException)
        {
            // 目录不存在也是正常情况
        }
        catch (Exception ex)
        {
            // 其他异常可以记录日志，但返回空列表保持可用性
            System.Diagnostics.Debug.WriteLine($"获取表情列表时出错: {ex.Message}");
        }

        return new List<string>();
    }

    /// <summary>
    /// 获取所有表情URL列表（同步版本，保持原接口兼容性）
    /// </summary>
    public static List<string> GetAllEmoji()
    {
        // 同步方法内部调用异步版本并等待结果
        return GetAllEmojiAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// 删除指定索引的表情
    /// </summary>
    public static async Task DeleteEmojiAsync(int index)
    {
        try
        {
            var emojiList = await GetAllEmojiAsync();

            if (index >= 0 && index < emojiList.Count)
            {
                emojiList.RemoveAt(index);

                // 使用 LocalCache 保存更新后的列表
                var result = await LocalCache.SaveJsonAsync(FilePath,
                    JsonSerializer.Serialize(emojiList),
                    createDirectory: false);

                if (!result.Success)
                {
                    throw new InvalidOperationException($"删除失败: {result.Message}");
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(index),
                    $"索引 {index} 超出范围，列表包含 {emojiList.Count} 个元素");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"删除表情时发生错误: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 删除指定索引的表情（同步版本）
    /// </summary>
    public static void DeleteEmoji(int index)
    {
        DeleteEmojiAsync(index).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 清空所有表情
    /// </summary>
    public static async Task ClearAllEmojiAsync()
    {
        try
        {
            // 保存空列表
            var result = await LocalCache.SaveJsonAsync(FilePath,
                JsonSerializer.Serialize(new List<string>()),
                createDirectory: false);

            if (!result.Success)
            {
                throw new InvalidOperationException($"清空失败: {result.Message}");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"清空表情时发生错误: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 清空所有表情（同步版本）
    /// </summary>
    public static void ClearAllEmoji()
    {
        ClearAllEmojiAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// 批量添加表情URL
    /// </summary>
    public static async Task<int> AddEmojisAsync(IEnumerable<string> urls)
    {
        if (urls == null)
            throw new ArgumentNullException(nameof(urls));

        var emojiList = await GetAllEmojiAsync();
        int addedCount = 0;

        foreach (var url in urls)
        {
            if (!string.IsNullOrWhiteSpace(url) && !emojiList.Contains(url))
            {
                emojiList.Add(url);
                addedCount++;
            }
        }

        if (addedCount > 0)
        {
            var result = await LocalCache.SaveJsonAsync(FilePath,
                JsonSerializer.Serialize(emojiList),
                createDirectory: false);

            if (!result.Success)
            {
                throw new InvalidOperationException($"批量添加失败: {result.Message}");
            }
        }

        return addedCount;
    }

    /// <summary>
    /// 检查表情URL是否已存在
    /// </summary>
    public static async Task<bool> ContainsEmojiAsync(string url)
    {
        var emojiList = await GetAllEmojiAsync();
        return emojiList.Contains(url);
    }

    /// <summary>
    /// 获取表情数量
    /// </summary>
    public static async Task<int> GetEmojiCountAsync()
    {
        var emojiList = await GetAllEmojiAsync();
        return emojiList.Count;
    }

    /// <summary>
    /// 更新指定索引的表情URL
    /// </summary>
    public static async Task UpdateEmojiAsync(int index, string newUrl)
    {
        if (string.IsNullOrWhiteSpace(newUrl))
            throw new ArgumentException("新的表情URL不能为空", nameof(newUrl));

        var emojiList = await GetAllEmojiAsync();

        if (index >= 0 && index < emojiList.Count)
        {
            // 检查新URL是否已存在（除了当前位置）
            for (int i = 0; i < emojiList.Count; i++)
            {
                if (i != index && emojiList[i] == newUrl)
                {
                    throw new InvalidOperationException($"表情URL已存在于位置 {i}");
                }
            }

            emojiList[index] = newUrl;

            var result = await LocalCache.SaveJsonAsync(FilePath,
                JsonSerializer.Serialize(emojiList),
                createDirectory: false);

            if (!result.Success)
            {
                throw new InvalidOperationException($"更新失败: {result.Message}");
            }
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(index),
                $"索引 {index} 超出范围");
        }
    }
}