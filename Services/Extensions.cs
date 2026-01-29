using Microsoft.UI.Xaml.Navigation;
using System;

namespace CC98.Services.Extensions;


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
