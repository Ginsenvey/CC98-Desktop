namespace CC98.Objects;

/// <summary>
/// 发送回复的请求体(ReplyToPost 模式,回复特定楼层,含 parentId)。
/// </summary>
public class SendReplyToPostRequest
{
	public int ClientType { get; set; } = 1;
	public string Content { get; set; } = "";
	public int ContentType { get; set; }
	public bool IsAnonymous { get; set; }
	public bool NotifyAllReplier { get; set; }
	public string Title { get; set; } = "";

	/// <summary>
	/// 所回复楼层的帖子 ID。
	/// </summary>
	public int ParentId { get; set; }
}