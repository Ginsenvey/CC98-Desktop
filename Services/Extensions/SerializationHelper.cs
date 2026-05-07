using System;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;

using CC98.Objects;

namespace CC98.Services.Extensions;

/// <summary>
/// 提供序列化相关的辅助方法。该类型为静态类型。
/// </summary>
public static class SerializationHelper
{
    /// <summary>
    /// 尝试从 JSON 字符串中反序列化数据。
    /// </summary>
    /// <typeparam name="T">需要反序列化的类型。</typeparam>
    /// <param name="json">需要反序列化的 JSON 字符串。</param>
    /// <returns><paramref name="json"/> 中包含的数据。如序列化失败，则返回 <typeparamref name="T"/> 类型的默认值。</returns>
    /// <remarks>此方法不会失败。如果发生任何异常，都将返回默认值。</remarks>
    public static T? TryDeserialize<T>(string json)
    {
        try
        {
            return (T?)JsonSerializer.Deserialize(json, typeof(T), CC98JsonContext.Default);
        }
        catch (Exception ex)
        {
            Trace.TraceError("序列化 JSON 字符串时发生错误，字符串 = {0}，类型 = {1}，错误 = {2}", json, typeof(T), ex.Message);
            return default;
        }
    }

    /// <summary>
    ///  尝试将数据对象序列化为 JSON 字符串。
    /// </summary>
    /// <typeparam name="T">需要序列化的数据类型。</typeparam>
    /// <param name="obj">需要序列化的对象。</param>
    /// <returns><paramref name="obj"/> 序列化后的 JSON 字符串。如序列化失败，则返回 <see cref="string.Empty"/>。</returns>
    /// <remarks>此方法不会失败。如果发生任何异常，都将返回默认值。</remarks>
    public static string TrySerialize<T>(T obj)
    {
        try
        {
            return JsonSerializer.Serialize(obj, typeof(T), CC98JsonContext.Default);
        }
        catch (Exception ex)
        {
            Trace.TraceError("序列化对象时发生错误，类型 = {0}，错误 = {1}", typeof(T), ex.Message);
            return string.Empty;
        }
    }
}