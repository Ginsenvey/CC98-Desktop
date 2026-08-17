namespace CC98.Objects;

/// <summary>
/// 发送回复的请求体(ReplyToTopic 模式,不含 parentId)。
/// 服务器仅在 ReplyToPost 模式接受 parentId 字段,故与 SendReplyToPostRequest 分离。
/// </summary>
public class SendReplyRequest
{
	public int ClientType { get; set; } = 1;
	public string Content { get; set; } = "";
	public int ContentType { get; set; }
	public bool IsAnonymous { get; set; }
	public bool NotifyAllReplier { get; set; }
	public string Title { get; set; } = "";
}