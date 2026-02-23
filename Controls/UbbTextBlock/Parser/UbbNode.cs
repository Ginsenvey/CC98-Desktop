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
}