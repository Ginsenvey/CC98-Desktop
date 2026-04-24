namespace CC98.Controls.UbbTextBlock.Parser;

//UBB类型

// 文本节点：用于存放纯文本内容
public class TextNode(string content) : UbbNode
{
    public string Content { get; } = content;
    public override UbbNodeType Type => UbbNodeType.Text;
}
