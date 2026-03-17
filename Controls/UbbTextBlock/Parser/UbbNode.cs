using System.Collections.Generic;
using System.Diagnostics;

namespace UbbRender.Parser;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public abstract class UbbNode
{
    public virtual UbbNodeType Type { get; protected init; }
    private readonly List<UbbNode> _children = new();
    public IReadOnlyList<UbbNode> Children => _children;
    public UbbNode Parent { get; set; } // 移除 init 以便在 AddChild 中赋值
    // 供调试器使用的属性
    protected virtual string DebuggerDisplay =>
        Children.Count > 0 ? $"[{Type}] (Children: {Children.Count})" : $"[{Type}]";
    public void AddChild(UbbNode child)
    {
        child.Parent = this;
        _children.Add(child);
    }

    public UbbNode? PreviousSibling
    {
        get
        {
            if (Parent == null)
                return null;

            var siblings = Parent._children; // 直接访问私有字段避免创建新列表
            var currentIndex = siblings.IndexOf(this);

            if (currentIndex > 0)
                return siblings[currentIndex - 1];

            return null;
        }
    }

    public UbbNode? NextSibling
    {
        get
        {
            if (Parent == null)
                return null;

            var siblings = Parent._children; // 直接访问私有字段避免创建新列表
            var currentIndex = siblings.IndexOf(this);

            if (currentIndex >= 0 && currentIndex < siblings.Count - 1)
                return siblings[currentIndex + 1];

            return null;
        }
    }

    /// <summary>
    /// 取得所有后代节点中指定类型的节点列表。可以选择是否包含当前节点本身。
    /// </summary>
    /// <param name="nodeType"></param>
    /// <param name="includeSelf"></param>
    /// <returns></returns>
    public List<UbbNode> GetDescendantsByType(UbbNodeType nodeType, bool includeSelf = false)
    {
        var result = new List<UbbNode>();

        if (includeSelf && this.Type == nodeType)
        {
            result.Add(this);
        }

        CollectDescendantsByType(this, nodeType, result);

        return result;
    }

    private static void CollectDescendantsByType(UbbNode node, UbbNodeType targetType, List<UbbNode> result)
    {
        foreach (var child in node._children)
        {
            if (child.Type == targetType)
            {
                result.Add(child);
            }

            CollectDescendantsByType(child, targetType, result);
        }
    }
    public UbbNode? FirstChild
    {
        get
        {
            return _children.Count > 0 ? _children[0] : null;
        }
    }
}