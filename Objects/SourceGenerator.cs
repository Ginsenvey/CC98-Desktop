using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CC98.Objects;

// 配置 System.Text.Json 的源生成器，提前为常用类型生成序列化器/反序列化器，保留与运行时选项一致的大小写不敏感行为。
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(Favorites))]
[JsonSerializable(typeof(List<Favorites>))]
[JsonSerializable(typeof(FavoritesInfo))]
[JsonSerializable(typeof(TopicInfo))]
[JsonSerializable(typeof(List<TopicInfo>))]
[JsonSerializable(typeof(BoardData))]
[JsonSerializable(typeof(Friend))]
[JsonSerializable(typeof(BasicUserInfo))]
[JsonSerializable(typeof(ReactionState))]
[JsonSerializable(typeof(UserInfo))]
[JsonSerializable(typeof(SimpleTopicInfo))]
[JsonSerializable(typeof(List<SimpleTopicInfo>))]
[JsonSerializable(typeof(Reply))]
[JsonSerializable(typeof(MediaContent))]
[JsonSerializable(typeof(ChatMessage))]
[JsonSerializable(typeof(ChatInfo))]
[JsonSerializable(typeof(VoteInfo))]
[JsonSerializable(typeof(VoteItem))]
[JsonSerializable(typeof(UnreadMessageInfo))]
[JsonSerializable(typeof(Notice))]
[JsonSerializable(typeof(PostBasicInfo))]
[JsonSerializable(typeof(CardStat))]
[JsonSerializable(typeof(Card))]
[JsonSerializable(typeof(CardStatInfoPair))]
[JsonSerializable(typeof(GachaInfo))]
[JsonSerializable(typeof(FlipTopic))]
[JsonSerializable(typeof(SectionCard))]
[JsonSerializable(typeof(IndexTopic))]
[JsonSerializable(typeof(BoardBest))]
[JsonSerializable(typeof(BasicTopicInfo))]
[JsonSerializable(typeof(BoardInfo))]
[JsonSerializable(typeof(SectionInfo))]
// 常见 ApiResponse 泛型特化（按项目中常见返回类型预生成）
[JsonSerializable(typeof(ApiResponse<string>))]
[JsonSerializable(typeof(ApiResponse<bool>))]
[JsonSerializable(typeof(ApiResponse<FavoritesInfo>))]
[JsonSerializable(typeof(ApiResponse<List<string>>))]
[JsonSerializable(typeof(ApiResponse<List<SimpleTopicInfo>>))]
[JsonSerializable(typeof(ApiResponse<TopicInfo>))]
internal partial class CC98JsonContext : JsonSerializerContext
{
}


