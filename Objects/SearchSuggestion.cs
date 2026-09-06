namespace CC98.Objects;

/// <summary>
/// 搜索建议的类型:决定选择建议后的跳转/行为。
/// </summary>
public enum SearchSuggestionType
{
    /// <summary>搜索话题(默认搜索行为)。</summary>
    Topic,
    /// <summary>按用户名搜索用户。</summary>
    User,
    /// <summary>按用户 ID 直接打开用户主页。</summary>
    UserId,
    /// <summary>按版面名打开版面。</summary>
    Board,
    /// <summary>按主题 ID 直接浏览帖子。</summary>
    TopicId
}

/// <summary>
/// AutoSuggestBox 下拉建议项。
/// </summary>
public class SearchSuggestion
{
    public SearchSuggestionType Type { get; set; }
    public string Title { get; set; } = "";

    /// <summary>次要说明文字(如用户 ID / 版面所属分区)。</summary>
    public string? Subtitle { get; set; }

    /// <summary>执行所需的参数(用户 ID / 主题 ID / 版面 ID / 用户名等)。</summary>
    public string? Parameter { get; set; }
}
