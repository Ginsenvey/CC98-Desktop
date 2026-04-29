namespace CC98.Objects;

public class RatingReason
{
    public bool Enabled { get; set; }
    public string Reason { get; set; } = "";

    public int Id { get; set; }

    // 加风评为1，扣风评为2
    public int Type { get; set; }
}

public class Rating
{
    public int ReasonId { get; set; }
    public int Type { get; set; }
}