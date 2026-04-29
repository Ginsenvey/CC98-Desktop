using System.Collections.Generic;

namespace CC98.Controls.UbbTextBlock.Parser;

public class UbbDocument
{
    private readonly List<UbbNode> _allNodes = [];

    public UbbDocument()
    {
        Root = TagNode.Create(UbbNodeType.Document);
        _allNodes.Add(Root);
    }

    public TagNode Root { get; init; }
    public IReadOnlyList<UbbNode> AllNodes => _allNodes;
}