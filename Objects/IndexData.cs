using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace CC98.Objects;

public class IndexData
{
    public List<IndexTopic> HotTopic { get; set; } = [];
    public List<IndexTopic> SchoolEvent { get; set; } = [];
    public List<IndexTopic> Academics { get; set; } = [];
    public List<IndexTopic> Study { get; set; } = [];
    public List<IndexTopic> Emotion { get; set; } = [];
    public List<IndexTopic> FleaMarket { get; set; } = [];
    public List<IndexTopic> FullTimeJob { get; set; } = [];
    public List<IndexTopic> PartTimeJob { get; set; } = [];

    public List<FlipTopic> RecommendationReading { get; set; } = [];
    public int TodayCount { get; set; }
    public int TodayTopicCount { get; set; }
    public int TopicCount { get; set; }
    public int UserCount { get; set; }
    public int OnlineUserCount { get; set; }
    public int PostCount { get; set; }
    public string LastUserName { get; set; } = string.Empty;

    public Dictionary<string, List<IndexTopic>> GetPartitions()
    {
        return new Dictionary<string, List<IndexTopic>>
        {
            ["HotTopic"] = HotTopic,
            ["Academics"] = Academics,
            ["Study"] = Study,
            ["SchoolEvent"] = SchoolEvent,
            ["Emotion"] = Emotion,
            ["FleaMarket"] = FleaMarket,
            ["FullTimeJob"] = FullTimeJob,
            ["PartTimeJob"] = PartTimeJob
        };
    }

}