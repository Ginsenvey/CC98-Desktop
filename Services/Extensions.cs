using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CC98.Objects;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Navigation;

namespace CC98.Services;


public static class NavigationEventArgsExtensions
{
    /// <summary>
    /// 尝试安全地获取导航参数
    /// </summary>
    /// <typeparam name="TParam">目标参数类型</typeparam>
    /// <param name="e">导航事件参数</param>
    /// <returns>转换后的参数，失败返回 default(TParam)</returns>
    public static TParam? TryGetParameter<TParam>(this NavigationEventArgs e)
    {
        if (e?.Parameter == null)
            return default;

        try
        {
            // 处理可空值类型的情况
            if (typeof(TParam).IsGenericType &&
                typeof(TParam).GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var underlyingType = Nullable.GetUnderlyingType(typeof(TParam));
                if (underlyingType != null && e.Parameter.GetType() == underlyingType)
                {
                    return (TParam)e.Parameter;
                }
            }

            return e.Parameter is TParam param ? param : default;
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// 获取导航参数，转换失败时抛出异常
    /// </summary>
    /// <typeparam name="TParam">目标参数类型</typeparam>
    /// <param name="e">导航事件参数</param>
    /// <returns>转换后的参数</returns>
    /// <exception cref="InvalidCastException">参数类型不匹配时抛出</exception>
    public static TParam GetParameter<TParam>(this NavigationEventArgs e)
    {
        if (e?.Parameter == null)
            throw new ArgumentNullException(nameof(e.Parameter), "导航参数为null");

        if (e.Parameter is TParam param)
            return param;

        throw new InvalidCastException(
            $"无法将导航参数从类型 '{e.Parameter.GetType().Name}' 转换为 '{typeof(TParam).Name}'");
    }

    /// <summary>
    /// 安全获取参数，提供默认值
    /// </summary>
    /// <typeparam name="TParam">目标参数类型</typeparam>
    /// <param name="e">导航事件参数</param>
    /// <param name="defaultValue">转换失败时的默认值</param>
    /// <returns>转换后的参数或默认值</returns>
    public static TParam GetParameterOrDefault<TParam>(this NavigationEventArgs e, TParam defaultValue = default)
    {
        return TryGetParameter<TParam>(e) ?? defaultValue;
    }

    /// <summary>
    /// 检查导航参数是否为指定类型
    /// </summary>
    /// <typeparam name="TParam">要检查的类型</typeparam>
    /// <param name="e">导航事件参数</param>
    /// <returns>如果参数是指定类型则返回 true</returns>
    public static bool IsParameterType<TParam>(this NavigationEventArgs e)
    {
        return e?.Parameter is TParam;
    }

    /// <summary>
    /// 获取参数的类型信息
    /// </summary>
    /// <param name="e">导航事件参数</param>
    /// <returns>参数的类型，如果为null则返回null</returns>
    public static Type? GetParameterType(this NavigationEventArgs e)
    {
        return e?.Parameter?.GetType();
    }
}

public static class ObjectExtensions
{
    public static int? ToNullableInt(this object obj)
    {
        if (obj == null || obj == DBNull.Value)
            return null;

        // 如果已经是 int 类型
        if (obj is int i)
            return i;

        // 如果可以直接转换
        if (obj is IConvertible convertible)
        {
            try
            {
                return convertible.ToInt32(null);
            }
            catch
            {
                // 继续尝试其他方法
            }
        }

        // 尝试解析字符串
        return int.TryParse(obj.ToString(), out var result) ? result : null;
    }

    public static int ToInt(this object obj, int defaultValue = 0)
    {
        return ToNullableInt(obj) ?? defaultValue;
    }
}


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

// 定义自己的扩展方法来避免冲突
public static class DispatcherQueueExtensions
{
    public static async Task EnqueueAsync(this DispatcherQueue dispatcher,
        Action action,
        DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
    {
        var tcs = new TaskCompletionSource<bool>();

        if (!dispatcher.TryEnqueue(priority, () =>
        {
            try
            {
                action();
                tcs.TrySetResult(true);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }))
        {
            tcs.TrySetException(new InvalidOperationException("Failed to enqueue the action"));
        }

        await tcs.Task;
    }
}
public class JsonSerialize
{
    private static readonly JsonSerializerOptions LogOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
        TypeInfoResolver = CC98JsonContext.Default 
    };
    /// <summary>
    /// 基于 <see cref="JsonSerializer"/> 的安全反序列化方法，用于日志的汉化
    /// LogOptions提供了支持源生成器的类型解析器，从而可以忽略AOT警告
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="json"></param>
    /// <returns></returns>
    public static T? Deserialize<T>(string json)
    {
        try 
        {
            var obj = JsonSerializer.Deserialize<T>(json, LogOptions);
            return obj is T result ? result : default;
        }
        catch 
        {
            return default;
        }
    }
    public static string Serialize<T>(T obj)
    {
        try
        {
            return JsonSerializer.Serialize(obj, LogOptions);
        }
        catch
        {
            return string.Empty;
        }
    }
}