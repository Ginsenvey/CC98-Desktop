using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CC98.Services.Extensions;

public static class LocalCacheExtensions
{
    /// <summary>
    /// 将当前缓存的内容保存到新路径
    /// </summary>
    public static async Task<(bool Success, string Message)> SaveToAsync(
        this LocalCache cache,
        string newPath,
        JsonSerializerContext? context = null)
    {
        return await LocalCache.SaveAsync(newPath, cache.Content, context);
    }

    /// <summary>
    /// 更新当前缓存文件
    /// </summary>
    public static async Task<(bool Success, string Message)> UpdateAsync<T>(
        this LocalCache cache,
        T newData,
        JsonSerializerContext? context = null)
    {
        return await LocalCache.SaveAsync(cache.CachePath, newData, context);
    }
}