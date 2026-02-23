using Microsoft.UI.Xaml.Navigation;
using System;
using Windows.Networking.NetworkOperators;

namespace CC98.Objects;

//导航参数类型
//对于只有一个参数的页面，不提供专有类型



/// <summary>
/// 导航信息的基类，提供一个属性以标识是否使用该导航信息。
/// 导航时上一次导航将会被全局保留，直到下一次导航时被覆盖或者被标记为不使用。
/// </summary>
public abstract class NavigationInfo
{
    public abstract bool ShouldUseThisInfo { get; set; }
    
    public Type PageType { get; set; }
    
}



public class TopicNavigationInfo: NavigationInfo
{
   
    public bool IsJumpingMode { get; set; } = false;
    public int TargetFloor { get; set; } = 0;
    public int TopicId {  get; set; }
    public override bool ShouldUseThisInfo { get; set; }=true;

    public TopicNavigationInfo()
    {
        PageType = typeof(Topic);
    }
}

public class ProfileNavigationInfo: NavigationInfo
{

    public int UserId {  get; set; }
    public bool IsMe {  get; set; }
    public override bool ShouldUseThisInfo { get; set; } = true;
    public ProfileNavigationInfo()
    {
        PageType = typeof(Profile);
    }
}
public class MessageNavigationInfo: NavigationInfo
{
    /// <summary>
    /// 标注以何种方式跳转到私信页面，从而让Chat页面做出响应。
    /// 如果从私信功能跳转(True)，那么MessageNavigationInfo将会传输要私信的用户信息
    /// </summary>
    public bool IsFromProfile { get; set; } = false;
    public ChatInfo ChatUserInfo {  get; set; } = new ChatInfo();
    public override bool ShouldUseThisInfo { get; set; } = true;
    public MessageNavigationInfo()
    {
        PageType = typeof(Message);
    }

}

public class SearchNavigationInfo: NavigationInfo
{
    public SearchType SearchType { get; set; }
    public string Key { get; set; } = string.Empty;
    public override bool ShouldUseThisInfo { get; set; } = true;
    public SearchNavigationInfo()
    {
        PageType = typeof(Search);
    }
}

public class EditorNavigationInfo: NavigationInfo
{
    public EditorMode EditorMode { get; set;} = EditorMode.ReplyToPost;
    public int TopicId { get; set;} = 0;
    public int PostId {  get; set;} = 0;

    public int BoardId { get; set; } = 0;
    //所回复的帖子Id
    public int ParentId {  get; set;} = 0;
    //引用头
    public string QuoteHeader {  get; set; } = string.Empty;

    //传入待编辑帖子的原文，或者签名档的原文
    public string BaseText {  get; set; } = string.Empty;
    //回复主题的标题,其他提示信息
    public string HintText { get; set; }= string.Empty;

    public override bool ShouldUseThisInfo { get; set; } = true;

    public EditorNavigationInfo()
    {
        PageType = typeof(UBBEditor);
    }
}


