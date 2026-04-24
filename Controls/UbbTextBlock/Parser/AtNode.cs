namespace CC98.Controls.UbbTextBlock.Parser;

/// <summary>
/// @提及节点
/// </summary>
public class AtNode(string username) : UbbNode
{
    public string Username { get; } = username;

    public override UbbNodeType Type => UbbNodeType.At;

}
