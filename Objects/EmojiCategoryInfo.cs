namespace CC98.Objects;

/// <summary>
/// 表情分类信息:Tag 对应 Assets/Emoji 目录,IconPath 为该系列第一张表情。
/// </summary>
public sealed class EmojiCategoryInfo
{
    public string Tag { get; init; } = "";
    public string Name { get; init; } = "";
    public string IconPath { get; init; } = "";
}
