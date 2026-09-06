using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using CC98.Objects;

namespace CC98.Services.Helpers;



/// <summary>
/// 表情包加载服务:扫描 Assets/Emoji 目录,带缓存,SketchPage 与 ChatPage 共用。
/// </summary>
public static class EmojiService
{
    private static readonly Dictionary<string, List<Emoji>> Cache = [];
    private static readonly Lock Lock = new();

    /// <summary>
    /// 表情分类定义:Tag 对应 Assets/Emoji 下的子目录名。
    /// </summary>
    private static readonly (string Tag, string Name)[] Categories =
    [
        ("CC98", "CC98"),
        ("ac-white", "AC娘"),
        ("tb", "贴吧"),
        ("ms", "雀魂"),
        ("em", "经典")
    ];

    /// <summary>
    /// 获取所有可用分类,每类用其第一张表情图作为图标。目录缺失的分类自动跳过。
    /// </summary>
    public static List<EmojiCategoryInfo> GetCategories()
    {
        var result = new List<EmojiCategoryInfo>();
        foreach (var (tag, name) in Categories)
        {
            var emojis = GetEmojis(tag);
            if (emojis.Count == 0) continue;
            result.Add(new EmojiCategoryInfo { Tag = tag, Name = name, IconPath = emojis[0].EmojiPath });
        }
        return result;
    }

    /// <summary>
    /// 获取指定类型的所有表情。目录缺失等异常时返回空列表,不抛异常。
    /// </summary>
    /// <param name="type">表情包类型,对应 Assets/Emoji 下的子目录名(如 CC98 / ac-white / tb)。</param>
    public static List<Emoji> GetEmojis(string type)
    {
        lock (Lock)
        {
            if (Cache.TryGetValue(type, out var cached)) return cached;

            try
            {
                var emojiPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Emoji", type);
                var files = Directory.GetFiles(emojiPath, "*", SearchOption.TopDirectoryOnly);
                var list = files.Select(file =>
                {
                    var filename = Path.GetFileName(file);
                    return new Emoji { EmojiName = filename.Split(".")[0].ToLower(), EmojiPath = file };
                }).ToList();
                Cache[type] = list;
                return list;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载表情失败: {ex.Message}");
                return [];
            }
        }
    }
}
