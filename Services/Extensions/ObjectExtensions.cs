using System;

namespace CC98.Services.Extensions;

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