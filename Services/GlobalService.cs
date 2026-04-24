namespace CC98.Services;

//用于保留当前应用启动期间的导航参数、滚动位置和全局对象

public class GlobalService
{
    private static GlobalService? _instance;
    private GlobalService(){}
    public static readonly object Lock= new();
    public static GlobalService Instance
    {
        get
        {
            lock (Lock)
            {
                return _instance ??= new();
            }
        }
    }
    public int MessageCount = 0;
    public int AtCount = 0;
    public int ReplyCount = 0;
    public int SystemCount = 0;
    public bool ShouldReplaceNavigationArgs = false;
    public object? NavigationAnchor {  get; set; }
}
