using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using CC98.Controls.Picture;
using CC98.Kernel;
using Microsoft.UI.Xaml.Media.Imaging;

namespace CC98.Controls.Primitives;

public class SmartImageLoader : IImageLoader
{
    // 单例实例
    private static SmartImageLoader? _instance;
    private static readonly object Lock = new();


    private static readonly ConcurrentDictionary<bool, SmartImageLoader> Instances = new();

    private static readonly ConcurrentDictionary<string, WeakReference<BitmapSource?>> Cache = new();

    public SmartImageLoader()
    {
    }

    public SmartImageLoader(bool lowRes)
    {
        LowRes = lowRes;
    }

    // 公共属性
    public bool LowRes { get; set; }

    public static SmartImageLoader Default
    {
        get
        {
            if (_instance == null)
                lock (Lock)
                {
                    _instance ??= new();
                }

            return _instance;
        }
    }

    public async Task<BitmapSource?> LoadImage(string src)
    {
        if (string.IsNullOrEmpty(src))
            return null;

        var cacheKey = (LowRes ? "lr:" : "hr:") + src;

        if (Cache.TryGetValue(cacheKey, out var weak) && weak.TryGetTarget(out var cached) && cached != null)
            return cached;

        BitmapSource? result = null;

        try
        {
            if (UrlEx.IsLocalPath(src))
                result = await UrlEx.LoadLocalImage(src);
            else if (UrlEx.IsWebUrl(src)) result = await UrlEx.LoadWebImageAsync(src, LowRes);
        }
        catch
        {
            result = null;
        }

        // 缓存
        if (result != null)
            Cache.AddOrUpdate(cacheKey, new WeakReference<BitmapSource?>(result),
                (k, old) => new(result));

        return result;
    }

    public static SmartImageLoader GetInstance(bool lowRes)
    {
        return Instances.GetOrAdd(lowRes, l => new(l));
    }

    public static void ClearCache()
    {
        Cache.Clear();
    }
}