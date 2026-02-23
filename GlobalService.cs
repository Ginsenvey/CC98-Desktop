using CC98.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
 
namespace CC98.Services;

//用于保留当前应用启动期间的导航参数、滚动位置和全局对象

public class GlobalService
{
    private static GlobalService? _instance;
    private GlobalService(){}
    public static readonly object _lock= new object();
    public static GlobalService Instance
    {
        get
        {
            lock (_lock)
            {
                return _instance ??= new GlobalService();
            }
        }
    }
    public int MessageCount = 0;
    public int AtCount = 0;
    public int ReplyCount = 0;
    public int SystemCount = 0;
    public NavigationInfo? NavigationInfo { get; set; }

}
