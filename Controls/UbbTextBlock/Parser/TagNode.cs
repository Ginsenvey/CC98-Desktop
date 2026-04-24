using System.Collections.Generic;

namespace UbbRender.Parser;

public class TagNode : UbbNode
{
    public IReadOnlyDictionary<string, string> Attributes { get; init; }

    public string GetAttribute(string key, string defaultValue = "") =>
        Attributes.TryGetValue(key, out var value) ? value : defaultValue;

    public static TagNode Create(UbbNodeType type, Dictionary<string, string> attributes = null)
    {
        return new()
        {
            Type = type,
            Attributes = attributes ?? []
        };
    }
}