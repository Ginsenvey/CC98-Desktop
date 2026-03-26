using CC98.Kernel.UserExperience;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace CC98.Share.Controls.Primitives;
public class SmartImageLoader : IImageLoader
{
    // 单例实例
    private static SmartImageLoader? _instance;
    private static readonly object _lock = new();

    // 公共属性
    public bool LowRes { get; set; } = false;

    public SmartImageLoader()
    {
    }

    public SmartImageLoader(bool lowRes)
    {
        LowRes = lowRes;
    }

    public static SmartImageLoader Default
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new SmartImageLoader();
                }
            }
            return _instance;
        }
    }

    
    private static readonly ConcurrentDictionary<bool, SmartImageLoader> _instances = new();

    public static SmartImageLoader GetInstance(bool lowRes)
    {
        return _instances.GetOrAdd(lowRes, l => new SmartImageLoader(l));
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
            if (UrlEx.IsLocalPath(src))
            {
                result = await UrlEx.LoadLocalImage(src);
            }
            else if (UrlEx.IsWebUrl(src))
            {
                result = await UrlEx.LoadWebImage(src, LowRes);
            }
        }
        catch
        {
            result = null;
        }

        // 缓存
        if (result != null)
        {
            _cache.AddOrUpdate(cacheKey, new WeakReference<BitmapSource?>(result),
                (k, old) => new WeakReference<BitmapSource?>(result));
        }

        return result;
    }

    public static void ClearCache()
    {
        _cache.Clear();
    }
}