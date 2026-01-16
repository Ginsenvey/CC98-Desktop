using System.Collections.Generic;

namespace CC98.Controls.UbbRenderer.Common;
// 节点类型枚举
public enum UbbNodeType
{
    Document,      // 文档根节点
    Text,          // 纯文本
    Bold,          // 粗体 [b]
    Italic,        // 斜体 [i]
    Underline,     // 下划线 [u]
    Strikethrough, // 删除线 [del]
    Size,          // 字体大小 [size]
    Font,          // 字体 [font]
    Color,         // 颜色 [color]
    Url,           // 链接 [url]
    Image,         // 图片 [img]
    Audio,         // 音频 [audio]
    Video,         // 视频 [video]
    Code,          // 代码块 [code]
    Quote,         // 引用 [quote]
    Align,         // 对齐 [align]
    Left,          // 左对齐 [left]
    Center,        // 居中 [center]
    Right,         // 右对齐 [right]
    List,          // 列表 [list]
    ListItem,      // 列表项 [*]
    Paragraph,     // 段落（自动生成）
    LineBreak,      // 换行
    Divider,        // 分隔线 [hr]
    Emoji          // 表情 [em]

}

// 节点基类
public abstract class UbbNode
{
    public UbbNodeType Type { get; protected set; }
    public List<UbbNode> Children { get; set; } = new();
    public UbbNode Parent { get; set; }

    public virtual void AddChild(UbbNode child)
    {
        child.Parent = this;
        Children.Add(child);
    }
}

// 文本节点
public class TextNode : UbbNode
{
    public string Content { get; set; }

    public TextNode(string content)
    {
        Type = UbbNodeType.Text;
        Content = content;
    }
}

// 标签节点（带属性）
public class TagNode : UbbNode
{
    public Dictionary<string, string> Attributes { get; set; } = new();

    public string GetAttribute(string key, string defaultValue = "")
    {
        return Attributes.TryGetValue(key, out var value) ? value : defaultValue;
    }

    protected TagNode(UbbNodeType type)
    {
        Type = type;
    }

    // 创建特定类型的标签节点
    public static TagNode Create(UbbNodeType type, Dictionary<string, string> attributes = null)
    {
        var node = new TagNode(type);
        if (attributes != null)
        {
            node.Attributes = attributes;
        }
        return node;
    }
}

// 文档树
public class UbbDocument
{
    public TagNode Root { get; set; }
    public List<UbbNode> AllNodes { get; private set; } = new();

    public UbbDocument()
    {
        Root = TagNode.Create(UbbNodeType.Document);
        AllNodes.Add(Root);
    }
}
