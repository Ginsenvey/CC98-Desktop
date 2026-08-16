namespace CC98.Objects;

public class RatingReason
{
    public bool Enabled { get; set; }
    public string Reason { get; set; } = "";

    public int Id { get; set; }

    // 加风评为1，扣风评为2
    public int Type { get; set; }

    /// <summary>
    /// 理由项在 UI 中显示的莫兰迪色(本地随机生成,不参与序列化)。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string ColorHex { get; set; } = "#888888";
}

public class Rating
{
    public int ReasonId { get; set; }
    public int Type { get; set; }
}