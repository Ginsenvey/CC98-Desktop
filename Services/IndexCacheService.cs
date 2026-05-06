using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using CC98.Kernel.Authorize;
using CC98.Objects;
using CC98.Services.Extensions;
using System.Text.Json.Serialization;
using CC98.Kernel;

namespace CC98.Services;

public class IndexDataService
{
    private const string CacheFileName = "index_data.json";
    private static IndexDataService? _instance;
    private static readonly object Lock = new();

    private IndexDataService()
    {
    }

    public static IndexDataService Instance
    {
        get
        {
            lock (Lock)
            {
                return _instance ??= new();
            }
        }
    }

    /// <summary>
    ///     从API获取数据并更新缓存
    /// </summary>
    public async Task<bool> RefreshFromApiAsync(string apiUrl)
    {
        try
        {
            var res = await RequestSender.Fetch<IndexData>(apiUrl);
            if (res.IsSuccess)
            {
                return await SaveToCacheAsync(res.Data!);
            }
            return false;
        }
        catch (Exception ex)
        {
            await App.Logger.WriteAsync("IndexDataService", "获取首页失败", ex.Message);
            return false;
        }
    }

    /// <summary>
    ///     从缓存读取数据
    /// </summary>
    public async Task<IndexData?> LoadFromCacheAsync()
    {
        try
        {
            var folder = ApplicationData.Current.LocalCacheFolder;
            var filePath = Path.Combine(folder.Path, CacheFileName);

            var cache = await LocalCache.CreateAsync(filePath);

            if (cache.IsAvailable && !string.IsNullOrWhiteSpace(cache.Content))
                return JsonSerialize.Deserialize<IndexData>(cache.Content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"读取缓存失败: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    ///     获取指定分区的帖子列表
    /// </summary>
    public async Task<List<IndexTopic>> GetTopicPartitionAsync(string partitionName, int? maxCount = null)
    {
        var cachedData = await LoadFromCacheAsync();

        if (cachedData != null&&cachedData.GetPartitions().TryGetValue(partitionName, out var partitionTopics))
        {
            var topics = partitionTopics ?? [];
            if (maxCount.HasValue) return [.. topics.Take(maxCount.Value)];
            return topics;
        }

        return [];
    }

    /// <summary>
    ///     获取推荐阅读列表
    /// </summary>
    public async Task<List<FlipTopic>> GetRecommendationReadingAsync(int? maxCount = null)
    {
        var cachedData = await LoadFromCacheAsync();

        if (cachedData != null)
        {
            var recommendations = cachedData.RecommendationReading;
            if (maxCount.HasValue) return [.. recommendations.Take(maxCount.Value)];
            return recommendations;
        }

        return [];
    }


    /// <summary>
    ///     清理缓存
    /// </summary>
    public async Task ClearCacheAsync()
    {
        try
        {
            var folder = ApplicationData.Current.LocalCacheFolder;
            var filePath = Path.Combine(folder.Path, CacheFileName);

            if (File.Exists(filePath)) File.Delete(filePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"清理缓存失败: {ex.Message}");
        }
    }


    #region 私有方法


    /// <summary>
    ///     保存到缓存
    /// </summary>
    private async Task<bool> SaveToCacheAsync(IndexData data)
    {
        try
        {
            var json = JsonSerialize.Serialize<IndexData>(data);

            var folder = ApplicationData.Current.LocalCacheFolder;
            var filePath = Path.Combine(folder.Path, CacheFileName);

            var (Success, Message) = await LocalCache.SaveJsonAsync(
                filePath,
                json,
                false);
            return Success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"保存缓存失败: {ex.Message}");
            return false;
        }
    }

    #endregion
}