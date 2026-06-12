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
using System.Threading;
using CC98.Kernel;
using System.Diagnostics;
using CC98.Services.Helpers;

namespace CC98.Services;

public class IndexDataService
{
    private const string CacheFileName = "index_data.json";
    public ApiService ApiService = App.Current.GetService<ApiService>();

    public static string CacheFilePath
    {
        get
        {
            var folder = ApplicationData.Current.LocalCacheFolder;
            return Path.Combine(folder.Path, CacheFileName);
        }
    }
    /// <summary>
    /// 受保护的构造方法。
    /// </summary>
    private IndexDataService()
    {
    }

    /// <summary>
    /// 获取该类型的实例。
    /// </summary>
    public static IndexDataService Instance { get; } = new();

    public readonly static (string Key, string DisplayName)[] sections =
[
    ("HotTopic", "十大话题"),
    ("SchoolEvent", "校园活动"),
    ("Academics", "学术通知"),
    ("Study", "学习天地"),
    ("Emotion", "感性·情感"),
    ("FleaMarket", "跳蚤市场"),
    ("FullTimeJob", "求职广场"),
    ("PartTimeJob", "实习兼职")
];
    /// <summary>
    ///     从API获取数据并更新缓存
    /// </summary>
    public async Task<bool> RefreshFromApiAsync(string apiUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var res = await ApiService.Fetch<IndexData>(apiUrl, cancellationToken);
            if (res.IsSuccess)
            {
                await SaveToCacheAsync(res.Data, cancellationToken);
                return true;
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
    public static async Task<IndexData?> LoadFromCacheAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var cache = new JsonFileCache<IndexData>(CacheFilePath);
            return await cache.GetDataAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await App.Logger.WriteAsync("IndexCacheService", "加载首页缓存失败", ex.Message);
        }
        return null;
    }

    /// <summary>
    ///     获取指定分区的帖子列表
    /// </summary>
    public static async Task<IEnumerable<SectionCard>> GetSectionsAsync(CancellationToken cancellationToken = default)
    {
        var data = await LoadFromCacheAsync(cancellationToken);
        var cards = new List<SectionCard>(8);
        if (data == null) return [];
        var parts = data.GetPartitions();
        foreach(var (Key, DisplayName) in sections)
        {
            cards.Add(new SectionCard
            {
                SectionName=DisplayName,
                HexColor=ColorEx.GenerateMorandiColorHex(),
                IndexTopics = parts[Key] ?? []  
            });
        }
        return cards;
    }
    
    /// <summary>
    ///     获取推荐阅读列表
    /// </summary>
    public static async Task<IEnumerable<FlipTopic>> GetRecommendationReadingAsync(int? maxCount = null, CancellationToken cancellationToken = default)
    {
        var cachedData = await LoadFromCacheAsync(cancellationToken);
        if (cachedData != null)
        {
            var recommendations = cachedData.RecommendationReading;
            if (maxCount.HasValue) return [.. recommendations.Take(maxCount.Value)];
            return recommendations;
        }

        return [];
    }


    /// <summary>
    /// 清理缓存
    /// </summary>
    public static async Task ClearCache()
    {
        try
        {
            var cache = new JsonFileCache<IndexData>(CacheFilePath);
            cache.Delete();
        }
        catch (Exception ex)
        {
            await App.Logger.WriteAsync("IndexCacheService", "清理首页缓存失败", ex.Message);
        }
    }


    #region 私有方法


    /// <summary>
    /// 保存到缓存
    /// </summary>
    private static async Task SaveToCacheAsync(IndexData data, CancellationToken cancellationToken = default)
    {
        try
        {
            var cache = new JsonFileCache<IndexData>(CacheFilePath);
            await cache.UpdateDataAsync(data, cancellationToken);
        }
        catch (Exception ex)
        {
            await App.Logger.WriteAsync("IndexCacheService", "保存首页缓存失败", ex.Message);
        }
    }

    #endregion
}