using System.Data;
using System.Text.RegularExpressions;
using static CC98.Kernel.ApiScope.ApiEndpoints;

namespace CC98.Kernel.ApiScope;
/// <summary>
/// API终结点定义
/// </summary>
public static class ApiEndpoints
{
    private const string Base = "https://api.cc98.org";
    private const string Oidc = "https://openid.cc98.org";

    public static class Forum
    {
        public static string Index() => $"{Base}/config/index";
    }
    /// <summary>
    /// 用户个人信息
    /// </summary>
    public static class User
    {
        /// <summary>
        /// 单个用户详细信息
        /// </summary>
        public static string UserProfile(bool isMe,int userId)
        {
            return isMe ? $"{Base}/me" : $"{Base}/user/{userId}";
        }
        /// <summary>
        /// 获取一系列用户的基本信息
        /// </summary>
        /// <param name="param">
        /// id=1&id=2&id=3...
        /// </param>
        public static string BasicUserInfoList(string param) => $"{Base}/user/basic?{param}";

        public static string UserInfoList(string param) => $"{Base}/user?{param}";


        /// <summary>
        /// 获取当前用户的好友列表
        /// </summary>
        /// <remarks>返回id列表而不是详细信息</remarks>
        public static string FreiendList(string type,int start) => $"{Base}/me/{type}?from={start}&size=10";
        /// <summary>
        /// 获取好友动态
        /// </summary>
        public static string Moment(int start) => $"{Base}/me/followee/topic?from={start}&size=20&order=0";
        /// <summary>
        /// 已收藏帖子的最近更新
        /// </summary> 
        public static string FavoriteTopicUpdate(int start) => $"{Base}/topic/me/favorite?from={start}&size=20&order=1";
        /// <summary>
        /// 收藏夹列表
        /// </summary>
        public static string FavoritesList() => $"{Base}/me/favorite-topic-group";
        public static string RecentChatUserList() => $"{Base}/message/recent-contact-users?from=0&size=10";

        public static string ChatHistory(int userId,int start) => $"{Base}/message/user/{userId}?from={start}&size=10";
        public static string SearchUser(string name) => $"{Base}/user/name/{name}";
        public static string UnreadMessage() => $"{Base}/me/unread-count";
        public static string SystemNotice(string typeName,int start) => $"{Base}/notification/{typeName}?from={start}&size=10";
    }
    public static class Post
    {
        /// <summary>
        /// 点赞状态获取
        /// </summary>
        public static string ReactionState(int postId) => $"{Base}/post/{postId}/like";
        public static string React(int postId) => $"{Base}/post/{postId}/like";
    }
    public static class Board
    {
        public static string AllBoards() => $"{Base}/Board/all";
        public static string BoardInfo(int boardId) => $"{Base}/board/{boardId}";
        /// <summary>
        /// 获取版面帖子
        /// </summary>
        /// <param name="isBest">精华帖模式</param>
        public static string TopicList(bool isBest, int boardId,int start)
        {
            return isBest? $"{Base}/topic/best/board/{boardId}?from={start}&size=20": $"{Base}/board/{boardId}/topic?from={start}&size=20";
        }

        public static string EditFocusBoards(int boardId) => $"{Base}/me/custom-board/{boardId}";
    }
    public static class Topic 
    {
        public static string RecentTopic(bool isMe,int userId, int start)
        {
            return isMe ? $"{Base}/me/recent-topic?from={start}/size=11": $"{Base}/user/{userId}/recent-topic?userid={userId}&from={start}&size=11";
        }
        public static string TopicInfo(int topicId) => $"{Base}/topic/{topicId}";
        /// <summary>
        /// 是否已收藏
        /// </summary>
        /// <returns>布尔值</returns>
        public static string IsFavorite(int topicId) => $"{Base}/topic/{topicId}/isfavorite";
        public static string ReplyList(int topicId, int start) => $"{Base}/Topic/{topicId}/post?from={start}&size=10";
        public static string NewTopicList(int start) => $"{Base}/topic/new?from={start}&size=20";
        public static string RandomTopicList() => $"{Base}/topic/random-recent?size=10";

        public static string SearchTopic(string key, int start) => $"{Base}/topic/search?keyword={key}&from={start}&size=20";
        public static string FavoriteTopicList(int start, int order,int groupId) => $"{Base}/topic/me/favorite?from={start}&size=11&order={order}&groupid={groupId}";
        public static string Vote(int topicId) => $"{Base}/topic/{topicId}/vote";
        public static string AddIntoFavorites(int topicId,int groupId) => $"{Base}/me/favorite/{topicId}?groupid={groupId}";
    }

    public static class OpenID
    {
        /// <summary>
        /// 鉴权服务起点
        /// </summary>
        public static string GetAuthorizeUrl() => $"{Oidc}/connect/authorize";
        /// <summary>
        /// 令牌获取地址
        /// </summary>
        /// <returns>包含ACT,RFT的Json.</returns>
        public static string GetTokenUrl() => $"{Oidc}/connect/token";
        
    }
}