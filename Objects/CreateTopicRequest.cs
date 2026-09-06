namespace CC98.Objects;

/// <summary>
/// 创建新主题的请求体。
/// </summary>
public class CreateTopicRequest
{
	public int ClientType { get; set; } = 1;
	public string Content { get; set; } = "";
	public int ContentType { get; set; }
	public bool IsAnonymous { get; set; }
	public bool NotifyPoster { get; set; }
	public string Title { get; set; } = "";
	public int Type { get; set; }
}