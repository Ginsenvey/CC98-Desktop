using System.Collections.Generic;

namespace UbbRender.Parser;

public class UbbDocument
{
    public TagNode Root { get; init; }
    private readonly List<UbbNode> _allNodes = [];
    public IReadOnlyList<UbbNode> AllNodes => _allNodes;

    public UbbDocument()
    {
        Root = TagNode.Create(UbbNodeType.Document);
        _allNodes.Add(Root);
    }
}