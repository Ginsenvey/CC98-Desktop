using Microsoft.UI.Xaml.Navigation;
using System;

namespace CC98.Objects;

//导航参数类型
//对于只有一个参数的页面，不提供专有类型

public class TopicNavigationInfo
{
    public bool IsJumpingMode { get; set; } = false;
    public int TargetFloor { get; set; } = 0;
    public int TopicId {  get; set; }
}

public class ProfileNavigationInfo
{
    public int UserId {  get; set; }
    public bool IsMe {  get; set; }
}
public class MessageNavigationInfo
{
    /// <summary>
    /// 标注以何种方式跳转到私信页面，从而让Chat页面做出响应。
    /// 如果从私信功能跳转(True)，那么MessageNavigationInfo将会传输要私信的用户信息
    /// </summary>
    public bool IsFromProfile { get; set; } = false;
    public ChatInfo ChatUserInfo {  get; set; } = new ChatInfo();
    
}
public enum SearchType
{
    Topic,
    User,
    Board,
    Guide //使用指南
}
public class SearchNavigationInfo
{
    public SearchType SearchType { get; set; }
    public string Key { get; set; } = string.Empty;
}


