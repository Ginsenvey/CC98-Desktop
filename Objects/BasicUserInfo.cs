namespace CC98.Objects;

/// <summary>
///     基础信息，用于从id获取头像
/// </summary>
public class BasicUserInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PortraitUrl { get; set; } = string.Empty;
}