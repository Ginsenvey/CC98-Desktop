namespace CC98.Controls.UbbTextBlock.Parser;
/// <summary>
/// 为行内数学公式提供特别的类型
/// </summary>
/// <param name="latex"></param>
/// <param name="isBlock"></param>
public class LatexNode(string latex, bool isBlock) : UbbNode
{
    public string Latex { get; } = latex;
    public bool IsBlock { get; } = isBlock;
    public override UbbNodeType Type => UbbNodeType.Latex;
}