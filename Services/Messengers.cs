using System;
using CC98.Objects;

/// <summary>
/// 用于在各个组件间传递消息的服务，如导航栏项目的添加。
/// </summary> 
namespace CC98.Services;

public class Messenger
{
    private static Messenger? _instance;
    public static Messenger Instance => _instance ??= new();
    public event Action<NavigationItem>? NavigationItemAdded;

    public void AddNavigationItem(NavigationItem item)
    {
        NavigationItemAdded?.Invoke(item);
    }
}