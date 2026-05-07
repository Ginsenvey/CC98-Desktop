using System;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace CC98.Objects;

/// <summary>
/// 为 <see cref="JsonSerializerContext"/> 提供扩展方法。该类型为静态类型。
/// </summary>
public static class JsonSerializerContextExtensions
{
    extension(JsonSerializerContext context)
    {
        /// <summary>
        /// 尝试获取指定类型的 <see cref="JsonTypeInfo{T}"/> 对象，如果不存在则抛出异常。
        /// </summary>
        /// <typeparam name="T">参与 JSON 序列化的实际类型。</typeparam>
        /// <returns><typeparamref name="T"/> 类型对应的 <see cref="JsonTypeInfo{T}"/> 类型对象。</returns>
        /// <exception cref="InvalidOperationException">无法在当前上下文中找到 <typeparamref name="T"/> 类型对应的 <see cref="JsonTypeInfo{T}"/> 对象。</exception>
        public JsonTypeInfo<T> GetTypeInfo<T>() => context.GetTypeInfo(typeof(T)) as JsonTypeInfo<T> ?? throw new InvalidOperationException($"无法获取类型 {typeof(T)} 的 JsonTypeInfo 对象。");
    }
}