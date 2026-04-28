namespace CC98.Kernel;

/// <summary>
/// 提供一些工具方法。该类型为静态类型。
/// </summary>
public static class Utility
{
    extension(string value)
    {
        /// <summary>
        /// 将字符串裁剪到指定长度。
        /// </summary>
        /// <param name="maxLength">要裁剪的最终长度。</param>
        /// <returns>裁剪后的字符串。如字符串原始长度少于 <paramref name="maxLength"/>，则保持不变。</returns>
        public string Cut(int maxLength)
        {
            return value.Length <= maxLength ? value : value[..maxLength];
        }
    }
}