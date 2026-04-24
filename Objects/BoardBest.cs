using System.Collections.Generic;

namespace CC98.Objects;

/// <summary>
/// 精华帖
/// </summary>
public class BoardBest
{
    public int Count { get; set; }
    public List<SimpleTopicInfo> Topics { get; set; } = [];
}

public class BasicTopicInfo
{
    public string Title { get; set; } = string.Empty;
    public int Id { get; set; }
}

public class BoardInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class SectionInfo
{
    public string Name { get; set; } = string.Empty;
    public List<string> Masters { get; set; } = new();
    public List<BoardInfo> Boards { get; set; } = new();
}
