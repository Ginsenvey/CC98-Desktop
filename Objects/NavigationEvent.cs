using CC98.Share.Controls.Primitives;
using System.Collections.Generic;

namespace CC98.Objects;

//导航参数类型
//对于只有一个参数的页面，不提供专有类型


public class TopicNavigationInfo
{
   
    public bool IsJumpingMode { get; set; } = false;
    public int TargetFloor { get; set; } = 0;
    public int TopicId {  get; set; }

    public bool GoToLatest {  get; set; }=false;
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
    public bool HasTarget { get; set; } = false;
    public ChatInfo ChatUserInfo {  get; set; } = new();

}

public class SearchNavigationInfo
{
    public SearchType SearchType { get; set; }
    public string Key { get; set; } = string.Empty;
}

public class SketchNavigationInfo
{
    public EditorMode EditorMode { get; set;} = EditorMode.ReplyToPost;
    public int TopicId { get; set;} = 0;
    public int PostId {  get; set;} = 0;
    public int Floor {  get; set;} = 0;
    public int ContentType { get; set; } = 0;
    public int BoardId { get; set; } = 0;
    //所回复的帖子Id
    public int ParentId {  get; set;} = 0;
    //引用头
    public string QuoteHeader {  get; set; } = string.Empty;

    //传入待编辑帖子的原文，或者签名档的原文
    public string BaseText {  get; set; } = string.Empty;
    //回复主题的标题,其他提示信息
    public string HintText { get; set; }= string.Empty;

}

public  class ViewerNavigationInfo
{
    public List<string> Urls { get; set; } = [];
    public MediaType Type { get; set; } 
    public int CurrentIndex { get; set; } = 0;
}
