using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using CC98.Kernel;
using CC98.Kernel.Network;
using Microsoft.Extensions.Logging.Abstractions;
using static CC98.Services.AppLog;
using static IndexDataService;

namespace CC98.Objects;

// 配置 System.Text.Json 的源生成器，提前为常用类型生成序列化器/反序列化器，保留与运行时选项一致的大小写不敏感行为。
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(AuthorizeResult))]
[JsonSerializable(typeof(Favorites))]
[JsonSerializable(typeof(FavoritesInfo))]
[JsonSerializable(typeof(TopicInfo))]
[JsonSerializable(typeof(BoardData))]
[JsonSerializable(typeof(Friend))]
[JsonSerializable(typeof(BasicUserInfo))]
[JsonSerializable(typeof(ReactionState))]
[JsonSerializable(typeof(UserInfo))]
[JsonSerializable(typeof(SimpleTopicInfo))]
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
[JsonSerializable(typeof(CachedHomeData))]
[JsonSerializable(typeof(HomeStatistics))]
[JsonSerializable(typeof(LogEntry))]
[JsonSerializable(typeof(CardStatInfoPair))]
[JsonSerializable(typeof(GachaInfo))]
[JsonSerializable(typeof(FlipTopic))]
[JsonSerializable(typeof(SectionCard))]
[JsonSerializable(typeof(SectionInfo))]
[JsonSerializable(typeof(IndexTopic))]
[JsonSerializable(typeof(VpnLoginResult))]
[JsonSerializable(typeof(BoardBest))]
[JsonSerializable(typeof(BasicTopicInfo))]
[JsonSerializable(typeof(BoardInfo))]
[JsonSerializable(typeof(SectionInfo))]
// 常见 ApiResponse 泛型特化（按项目中常见返回类型预生成）
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(List<Reply>))]
[JsonSerializable(typeof(List<SectionInfo>))]
[JsonSerializable(typeof(List<BasicUserInfo>))]
[JsonSerializable(typeof(List<ChatInfo>))]
[JsonSerializable(typeof(List<LogEntry>))]
[JsonSerializable(typeof(List<SimpleTopicInfo>))]
[JsonSerializable(typeof(List<IndexTopic>))]
[JsonSerializable(typeof(List<FlipTopic>))]
[JsonSerializable(typeof(List<BoardInfo>))]
[JsonSerializable(typeof(List<TopicInfo>))]
[JsonSerializable(typeof(List<Favorites>))]
[JsonSerializable(typeof(List<ChatMessage>))]
[JsonSerializable(typeof(List<SectionCard>))]
[JsonSerializable(typeof(List<Notice>))]
[JsonSerializable(typeof(List<BasicTopicInfo>))]
[JsonSerializable(typeof(List<Card>))]
internal partial class CC98JsonContext : JsonSerializerContext
{
}


