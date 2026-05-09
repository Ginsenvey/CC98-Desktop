namespace CC98.Services;

//用于保留当前应用启动期间的导航参数、滚动位置和全局对象

public class GlobalService
{
    /// <summary>
    /// 当前用户的 @ 消息总数。
    /// </summary>
    public int AtCount { get; set; }

    /// <summary>
    /// 当前用户的私信消息总数。
    /// </summary>
    public int MessageCount { get; set; }

    /// <summary>
    /// 当前用户的回复消息总数。
    /// </summary>
    public int ReplyCount { get; set; }

    /// <summary>
    /// 当前用户的系统消息总数。
    /// </summary>
    public int SystemCount { get; set; }

    /// <summary>
    /// 是否替换导航参数。
    /// </summary>
    // TODO: 这个属性的命名可能需要更改，以更好地反映其用途。
    public bool ShouldReplaceNavigationArgs { get; set; }


    /// <summary>
    /// 受保护的构造方法。
    /// </summary>
    private GlobalService()
    {
    }

    /// <summary>
    /// 获取当前对象的唯一实例。
    /// </summary>
    public static GlobalService Instance { get; } = new();

    /// <summary>
    /// 全局导航参数对象，可以在应用的不同部分使用和修改，以便在导航时传递数据。
    /// </summary>
    public object? NavigationAnchor { get; set; }
}