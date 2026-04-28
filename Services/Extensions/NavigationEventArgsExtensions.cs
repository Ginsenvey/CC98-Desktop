using System;
using Microsoft.UI.Xaml.Navigation;

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
        if (e.Parameter == null)
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
    
}

// 定义自己的扩展方法来避免冲突