using CC98.Kernel.UserExperience;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Threading.Tasks;
using Windows.Media.Core;
using System;
using System.Collections.Concurrent;

namespace CC98.Share.Controls.Primitives;

public class SmartImageLoader : IImageLoader
{
    // 公共属性
    public bool LowRes { get; set; } = false;

    // 无参构造函数
    public SmartImageLoader()
    {
    }

    // 带参构造函数
    public SmartImageLoader(bool lowRes)
    {
        LowRes = lowRes;
    }

    private static readonly ConcurrentDictionary<string, WeakReference<BitmapSource?>> _cache = new();

    
    public async Task<BitmapSource?> LoadImage(string src)
    {
        if (string.IsNullOrEmpty(src))
            return null;

        var cacheKey = (LowRes ? "lr:" : "hr:") + src;

        if (_cache.TryGetValue(cacheKey, out var weak) && weak.TryGetTarget(out var cached) && cached != null)
        {
            return cached;
        }

        BitmapSource? result = null;

        try
        {
            if (ImageResolver.IsLocalPath(src))
            {
                result= await ImageResolver.LoadLocalImage(src);
            }
            else if (ImageResolver.IsWebUrl(src))
            {
                result = await ImageResolver.LoadWebImage(src, LowRes);
            }
            else
            {
                result = null;
            }
        }
        catch
        {
            result = null;
        }

        // 缓存
        try
        {
            _cache.AddOrUpdate(cacheKey, new WeakReference<BitmapSource?>(result), (k, old) => new WeakReference<BitmapSource?>(result));
        }
        catch
        {
            //忽略
        }

        return result;
    }
}