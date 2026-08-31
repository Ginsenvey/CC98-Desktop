namespace CC98.Kernel;

/// <summary>
///     API终结点定义
/// </summary>
public static class ApiEndpoints
{
    private const string Base = "https://api.cc98.org";
    private const string Oidc = "https://openid.cc98.org";
    private const string Card = "https://card.cc98.org";

    /// <summary>
    /// 提供论坛相关功能的 API 地址。该类型为静态类型。
    /// </summary>
    public static class Forum
    {
        public static string AppCenter => Oidc;

        /// <summary>
        /// 获取所有版面信息的 API 地址。
        /// </summary>
        public static string AllBoards => $"{Base}/Board/all";

        /// <summary>
        /// 获取论坛首页相关数据的 API 地址。
        /// </summary>
        public static string Index => $"{Base}/config/index";

        /// <summary>
        /// 获取抽卡统计信息的 API 地址。
        /// </summary>
        public static string CardStat => $"{Card}/api/collection/stat";

        /// <summary>
        /// 获取特定抽卡规则的 API 地址。
        /// </summary>
        /// <param name="ruleId">抽卡规则的标识。</param>
        /// <returns><paramref name="ruleId"/> 对应的抽卡规则的 API 地址。</returns>
        public static string DrawCard(int ruleId)
        {
            return $"{Card}/api/draw/{ruleId}";
        }

        /// <summary>
        /// 获取分解所有多余卡牌的 API 地址。
        /// </summary>
        public static string DestroyAllCards => $"{Card}/api/collection/all-rest";

        /// <summary>
        /// 获取上传文件的 API 地址。
        /// </summary>
        public static string UploadFile => $"{Base}/file";
    }


    /// <summary>
    ///     用户个人信息
    /// </summary>
    public static class User
    {
        public static string BrowseHistory(int start) => $"{Base}/me/browsing-record?from={start}&size=11";
        public static string EnableBrowseHistory(bool enabled) => $"{Base}/me/browsing-history?enabled={enabled.ToString().ToLower()}";
        public static string SendPrivateMessage => $"{Base}/message";

        /// <summary>
        ///     单个用户详细信息
        /// </summary>
        public static string UserProfile(bool isMe, int userId=0)
        {
            return isMe ? $"{Base}/me" : $"{Base}/user/{userId}";
        }

        public static string SignIn()
        {
            return $"{Base}/me/signin";
        }

        /// <summary>
        ///     获取一系列用户的基本信息
        /// </summary>
        /// <param name="param">
        ///     id=1&id=2&id=3...
        /// </param>
        public static string BasicUserInfoList(string param)
        {
            return $"{Base}/user/basic?{param}";
        }

        public static string UserInfoList(string param)
        {
            return $"{Base}/user?{param}";
        }


        /// <summary>
        ///     获取当前用户的好友列表
        /// </summary>
        /// <remarks>返回id列表而不是详细信息</remarks>
        public static string FreiendList(string type, int start)
        {
            return $"{Base}/me/{type}?from={start}&size=10";
        }

        /// <summary>
        ///     获取好友动态
        /// </summary>
        public static string Moment(int start)
        {
            return $"{Base}/me/followee/topic?from={start}&size=20&order=0";
        }

        /// <summary>
        ///     已收藏帖子的最近更新
        /// </summary>
        public static string FavoriteTopicUpdate(int start)
        {
            return $"{Base}/topic/me/favorite?from={start}&size=20&order=1";
        }

        /// <summary>
        ///     收藏夹列表
        /// </summary>
        public static string FavoritesList()
        {
            return $"{Base}/me/favorite-topic-group";
        }

        public static string RecentChatUserList(int start)
        {
            return $"{Base}/message/recent-contact-users?from={start}&size=10";
        }

        public static string ChatHistory(int userId, int start)
        {
            return $"{Base}/message/user/{userId}?from={start}&size=10";
        }

        public static string SearchUserByName(string name)
        {
            return $"{Base}/user/name/{name}";
        }

        public static string UnreadMessage()
        {
            return $"{Base}/me/unread-count";
        }

        public static string SystemNotice(string typeName, int start)
        {
            return $"{Base}/notification/{typeName}?from={start}&size=10";
        }
        //取关和关注
        public static string EditFollowee(int userId)
        {
            return $"{Base}/me/followee/{userId}";
        }

        public static string TransferWealth()
        {
            return $"{Base}/me/transfer-wealth";
        }

        
    }

    public static class Post
    {
        /// <summary>
        ///     点赞状态获取
        /// </summary>
        public static string ReactionState(int postId)
        {
            return $"{Base}/post/{postId}/like";
        }

        public static string React(int postId)
        {
            return $"{Base}/post/{postId}/like";
        }

        public static string Edit(int postId)
        {
            return $"{Base}/post/{postId}";
        }

        public static string Rate(int postId)
        {
            return $"{Base}/post/{postId}/rating-v2";
        }

        public static string RateReason(int type)
        {
            return $"{Base}/post/rating-reason?type={type}";
        }
    }

    public static class Board
    {
        public static string BoardInfo(int boardId)
        {
            return $"{Base}/board/{boardId}";
        }

        /// <summary>
        ///     获取版面帖子
        /// </summary>
        public static string TopicList(int type, int boardId, int start)
        {
            return type switch
            {
                0 => $"{Base}/board/{boardId}/topic?from={start}&size=20",
                1 => $"{Base}/topic/toptopics?boardid={boardId}",
                _ => $"{Base}/topic/best/board/{boardId}?from={start}&size=20"
            };
        }

        public static string EditFocusBoards(int boardId)
        {
            return $"{Base}/me/custom-board/{boardId}";
        }

        public static string WebUrl(int boardId)
        {
            return $"https://www.cc98.org/board/{boardId}";
        }

        public static string SendNewTopic(int boardId)
        {
            return $"{Base}/board/{boardId}/topic";
        }
        public static string Tags(int boardId)
        {
            return $"{Base}/board/{boardId}/tags";
        }
    }

    public static class Topic
    {
        /// <summary>
        /// 中文关键词使用Util.UrlEncode编码
        /// </summary>
        /// <param name="boardId"></param>
        /// <param name="key"></param>
        /// <param name="start"></param>
        /// <returns></returns>
        public static string SearchTopicInBoard(int boardId, string key, int start)
        {
            return $"{Base}/topic/search/board/{boardId}?keyword={key}&from={start}&size=20";
        }

        public static string RecentTopic(bool isMe, int userId, int start)
        {
            return isMe
                ? $"{Base}/me/recent-topic?from={start}&size=11"
                : $"{Base}/user/{userId}/recent-topic?userid={userId}&from={start}&size=11";
        }

        public static string TopicInfo(int topicId)
        {
            return $"{Base}/topic/{topicId}";
        }

        /// <summary>
        ///     是否已收藏
        /// </summary>
        /// <returns>布尔值</returns>
        public static string IsFavorite(int topicId)
        {
            return $"{Base}/topic/{topicId}/isfavorite";
        }

        public static string ReplyList(int topicId, int start)
        {
            return $"{Base}/Topic/{topicId}/post?from={start}&size=10";
        }

        public static string NewTopicList(int start)
        {
            return $"{Base}/topic/new?from={start}&size=20";
        }

        public static string RandomTopicList()
        {
            return $"{Base}/topic/random-recent?size=10";
        }

        public static string SearchTopic(string key, int start)
        {
            return $"{Base}/topic/search?keyword={key}&from={start}&size=20";
        }

        public static string FavoriteTopicList(int start, int order, int groupId)
        {
            return $"{Base}/topic/me/favorite?from={start}&size=11&order={order}&groupid={groupId}";
        }

        public static string Vote(int topicId)
        {
            return $"{Base}/topic/{topicId}/vote";
        }

        public static string AddIntoFavorites(int topicId, int groupId)
        {
            return $"{Base}/me/favorite/{topicId}?groupid={groupId}";
        }

        public static string BasicTopicInfoList(string param)
        {
            return $"{Base}/topic/basic?{param}";
        }

        public static string SendReply(int topicId)
        {
            return $"{Base}/topic/{topicId}/post";
        }
        public static string DeleteFavoriteTopic(int topicId)
        {
            return $"{Base}/me/favorite/{topicId}";
        }
    }

    public static class OpenId
    {
        public static string Endpoint => Oidc;
        /// <summary>
        ///     鉴权服务起点,用于产生跳转URL
        /// </summary>
        public static string AuthorizeUrl()
        {
            return $"{Oidc}/connect/authorize";
        }

        /// <summary>
        ///     令牌获取地址
        /// </summary>
        /// <returns>包含ACT,RFT的Json.</returns>
        public static string TokenEndpoint()
        {
            return $"{Oidc}/connect/token";
        }
    }
}