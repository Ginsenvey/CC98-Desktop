namespace CC98.Objects;

/// <summary>
/// 动态页面的内容类型
/// </summary>
public enum FocusContentType
{
    Followee = 0,
    FavoriteUpdata = 1
}
/// <summary>
/// 赞/踩
/// </summary>
public enum ReactionType
{
    Like = 0,
    Dislike = 1
}

public enum PostOrder
{
    Time = 0,      // 按发帖时间排序
    LastReply = 1, // 按最后回复时间排序
    Mark = 2       // 按收藏顺序排序
}

public enum EditorMode
{
    ReplyToPost = 0,//回复跟帖
    ReplyToTopic = 1,//回复主题作者
    DraftNewTopic = 2,//发表新主题
    EditMyPost = 3,//编辑已发出的帖子
    EditMyTopic = 4,
    EditSignatureCode = 5,//编辑签名档
    Chat = 6,//回复私信
}


public enum NoticeType
{
    System=1,
    At=3,
    Reply=2
}