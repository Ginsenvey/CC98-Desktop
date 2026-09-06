namespace CC98.Objects;

/// <summary>
/// 编辑帖子的请求体。
/// </summary>
public class EditPostRequest
{
	public int Type { get; set; }
	public string Content { get; set; } = "";
	public int ContentType { get; set; }
	public bool NotifyPoster { get; set; }
	public string Title { get; set; } = "";
}