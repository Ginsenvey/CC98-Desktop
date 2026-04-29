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
            var res = await LoginService.Vpn.GetAsync(apiUrl);
            var jsonResponse = await res.Content.ReadAsStringAsync();
            var homeData = ParseAndExtractData(jsonResponse);
            return await SaveToCacheAsync(homeData);
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
    public async Task<CachedIndexData?> LoadFromCacheAsync()
    {
        try
        {
            var folder = ApplicationData.Current.LocalCacheFolder;
            var filePath = Path.Combine(folder.Path, CacheFileName);

            var cache = await LocalCache.CreateAsync(filePath);

            if (cache.IsAvailable && !string.IsNullOrWhiteSpace(cache.Content))
                return JsonSerialize.Deserialize<CachedIndexData>(cache.Content);
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

        if (cachedData != null &&
            cachedData.TopicPartitions.TryGetValue(partitionName, out var topics))
        {
            if (maxCount.HasValue) return topics.Take(maxCount.Value).ToList();
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
            if (maxCount.HasValue) return recommendations.Take(maxCount.Value).ToList();
            return recommendations;
        }

        return [];
    }

    /// <summary>
    ///     获取统计数据
    /// </summary>
    public async Task<ForumStatistics?> GetStatisticsAsync()
    {
        var cachedData = await LoadFromCacheAsync();
        return cachedData?.Statistics;
    }

    /// <summary>
    ///     获取所有分区的名称
    /// </summary>
    public static List<string> GetAllPartitionNames()
    {
        return Partitions.All.ToList();
    }

    /// <summary>
    ///     检查缓存是否有效（例如在指定时间内）
    /// </summary>
    public async Task<bool> IsCacheValidAsync(TimeSpan maxAge)
    {
        var cachedData = await LoadFromCacheAsync();

        if (cachedData == null)
            return false;

        var age = DateTime.Now - cachedData.LastUpdateTime;
        return age <= maxAge;
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

    // 缓存数据结构（只包含需要的字段）
    public class CachedIndexData
    {
        public Dictionary<string, List<IndexTopic>> TopicPartitions { get; set; } = new();
        public List<FlipTopic> RecommendationReading { get; set; } = [];
        public ForumStatistics Statistics { get; set; } = new();
        public DateTime LastUpdateTime { get; set; }
    }

    // 统计数据模型
    public class ForumStatistics
    {
        public int TodayCount { get; set; }
        public int TodayTopicCount { get; set; }
        public int TopicCount { get; set; }
        public int UserCount { get; set; }
        public int OnlineUserCount { get; set; }
        public int PostCount { get; set; }
        public string LastUserName { get; set; } = string.Empty;
    }

    // 分区名称常量
    public static class Partitions
    {
        public const string HotTopic = "hotTopic";
        public const string SchoolEvent = "schoolEvent";
        public const string Academics = "academics";
        public const string Study = "study";
        public const string Emotion = "emotion";
        public const string FleaMarket = "fleaMarket";
        public const string FullTimeJob = "fullTimeJob";
        public const string PartTimeJob = "partTimeJob";

        public static readonly string[] All =
        [
            HotTopic, SchoolEvent, Academics, Study,
            Emotion, FleaMarket, FullTimeJob, PartTimeJob
        ];
    }

    #region 私有方法

    /// <summary>
    ///     解析JSON并提取所需数据
    /// </summary>
    private CachedIndexData ParseAndExtractData(string jsonResponse)
    {
        using var doc = JsonDocument.Parse(jsonResponse);
        var root = doc.RootElement;

        var result = new CachedIndexData
        {
            LastUpdateTime = DateTime.Now
        };

        // 1. 提取统计数据
        result.Statistics = ExtractStatistics(root);

        // 2. 提取推荐阅读
        result.RecommendationReading = ExtractRecommendationReading(root);

        // 3. 提取各分区帖子
        result.TopicPartitions = ExtractTopicPartitions(root);

        return result;
    }

    /// <summary>
    ///     提取统计数据
    /// </summary>
    private ForumStatistics ExtractStatistics(JsonElement root)
    {
        var stats = new ForumStatistics();

        if (root.TryGetProperty("todayCount", out var todayCount))
            stats.TodayCount = todayCount.GetInt32();

        if (root.TryGetProperty("todayTopicCount", out var todayTopicCount))
            stats.TodayTopicCount = todayTopicCount.GetInt32();

        if (root.TryGetProperty("topicCount", out var topicCount))
            stats.TopicCount = topicCount.GetInt32();

        if (root.TryGetProperty("userCount", out var userCount))
            stats.UserCount = userCount.GetInt32();

        if (root.TryGetProperty("onlineUserCount", out var onlineUserCount))
            stats.OnlineUserCount = onlineUserCount.GetInt32();

        if (root.TryGetProperty("postCount", out var postCount))
            stats.PostCount = postCount.GetInt32();

        if (root.TryGetProperty("lastUserName", out var lastUserName))
            stats.LastUserName = lastUserName.GetString() ?? string.Empty;

        return stats;
    }

    /// <summary>
    ///     提取推荐阅读
    /// </summary>
    private List<FlipTopic> ExtractRecommendationReading(JsonElement root)
    {
        var result = new List<FlipTopic>();

        if (root.TryGetProperty("recommendationReading", out var recommendations) &&
            recommendations.ValueKind == JsonValueKind.Array)
            foreach (var item in recommendations.EnumerateArray())
            {
                var topic = new FlipTopic();

                if (item.TryGetProperty("title", out var title))
                    topic.Title = title.GetString() ?? string.Empty;

                if (item.TryGetProperty("url", out var url))
                    topic.Url = url.GetString() ?? string.Empty;

                if (item.TryGetProperty("time", out var time))
                    topic.Time = time.GetString() ?? string.Empty;

                if (item.TryGetProperty("content", out var content))
                    topic.Content = content.GetString() ?? string.Empty;

                result.Add(topic);
            }

        return result;
    }

    /// <summary>
    ///     提取各分区帖子
    /// </summary>
    private Dictionary<string, List<IndexTopic>> ExtractTopicPartitions(JsonElement root)
    {
        var partitions = new Dictionary<string, List<IndexTopic>>();

        foreach (var partitionName in Partitions.All)
            if (root.TryGetProperty(partitionName, out var partition) &&
                partition.ValueKind == JsonValueKind.Array)
            {
                var topics = new List<IndexTopic>();

                foreach (var item in partition.EnumerateArray())
                {
                    var topic = new IndexTopic();

                    // 提取通用字段
                    if (item.TryGetProperty("title", out var title))
                        topic.Title = title.GetString() ?? string.Empty;

                    if (item.TryGetProperty("id", out var id))
                        topic.Id = id.GetInt32();

                    // 只有hotTopic分区有BoardName字段
                    if (partitionName == Partitions.HotTopic &&
                        item.TryGetProperty("boardName", out var boardName))
                        topic.BoardName = boardName.GetString() ?? string.Empty;

                    // 标记是否为热门话题
                    topic.IsHotTopic = partitionName == Partitions.HotTopic;

                    topics.Add(topic);
                }

                partitions[partitionName] = topics;
            }

        return partitions;
    }

    /// <summary>
    ///     保存到缓存
    /// </summary>
    private async Task<bool> SaveToCacheAsync(CachedIndexData data)
    {
        try
        {
            var json = JsonSerialize.Serialize<CachedIndexData>(data);

            var folder = ApplicationData.Current.LocalCacheFolder;
            var filePath = Path.Combine(folder.Path, CacheFileName);

            var result = await LocalCache.SaveJsonAsync(
                filePath,
                json,
                false);
            return result.Success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"保存缓存失败: {ex.Message}");
            return false;
        }
    }

    #endregion
}