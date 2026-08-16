using CC98.Services.Helpers;

// UBB→MD 新旧转换器对比验证工具:逐个样例对比 UbbToMd(正则) 与 UbbToMarkdown(AST) 的输出。
var samples = new List<(string Name, string Ubb)>
{
    ("纯文本", "你好世界"),
    ("换行", "第一行\n第二行\r\n第三行"),
    ("粗体", "这是[b]粗体[/b]文本"),
    ("斜体", "这是[i]斜体[/i]文本"),
    ("删除线", "这是[del]删除线[/del]文本"),
    ("下划线", "这是[u]下划线[/u]文本"),
    ("粗体跨段", "[b]第一段\n\n第二段[/b]"),
    ("颜色", "这是[color=red]红字[/color]"),
    ("颜色嵌套", "[color=red]外层[color=blue]内层[/color]结束[/color]"),
    ("字体", "[font=宋体]字体[/font]"),
    ("字号", "[size=3]字号[/size]"),
    ("链接带标题", "[url=https://cc98.org]论坛[/url]"),
    ("链接无标题", "[url]https://cc98.org[/url]"),
    ("图片", "[img]https://cc98.org/a.png[/img]"),
    ("图片隐藏", "[img]https://cc98.org/a.png[/img]"),
    ("代码块", "[code]\nvar x = 1;\n[/code]"),
    ("引用", "[quote]引用内容[/quote]"),
    ("嵌套引用", "[quote]外层[quote]内层[/quote]结尾[/quote]"),
    ("列表", "[list][*]项目一[*]项目二[/list]"),
    ("表格", "[table][tr][td]A[/td][td]B[/td][/tr][tr][td]1[/td][td]2[/td][/tr][/table]"),
    ("表情ac", "[ac01]"),
    ("表情em", "[em01]"),
    ("表情cc98", "[cc9801]"),
    ("表情贴吧", "[tb01]"),
    ("居中", "[center]居中内容[/center]"),
    ("右对齐", "[right]右对齐[/right]"),
    ("音频", "[audio]https://x.com/a.mp3[/audio]"),
    ("视频", "[video]https://x.com/v.mp4[/video]"),
    ("文件", "[upload]https://x.com/f.zip[/upload]"),
    ("哔哩", "[bili]BV1xx411c7mD[/bili]"),
    ("at", "你好@安安发米姬 世界"),
    ("latex", "公式$x^2$结束"),
    ("latex块", "$$x^2$$"),
    ("math标签", "[math]x^2[/math]"),
    ("line", "上[line]下"),
    ("topic", "[topic]话题[/topic]"),
    ("replyview", "[replyview]隐藏内容[/replyview]"),
    ("needreply", "[needreply]回复可见[/needreply]"),
    ("md", "[md]# 标题[/md]"),
    ("noubb", "[noubb][b]原样[/b][/noubb]"),
    ("混合1", "标题[b]粗[url=https://cc98.org]链接[/url]体[/b]结束"),
    ("混合2", "[quote]引用[b]粗体[/b]和[img]https://x.com/i.png[/img][/quote]"),
    ("br标签", "第一行<br>第二行"),
    ("未知标签", "这是[abc]未知标签[/abc]"),
    ("非法标签", "文本[url=https://a]未闭合"),
    ("转义标记", "plain-text-plus"),
};

var mismatches = 0;
foreach (var (name, ubb) in samples)
{
    var old1 = UbbToMd.Convert(ubb, true);
    var new1 = UbbToMarkdown.Convert(ubb, true);
    var old2 = UbbToMd.Convert(ubb, false);
    var new2 = UbbToMarkdown.Convert(ubb, false);

    var same = old1 == new1 && old2 == new2;
    if (!same) mismatches++;

    Console.WriteLine($"===== {name} =====");
    Console.WriteLine($"UBB : {Escape(ubb)}");
    if (same)
    {
        Console.WriteLine($"OK  : {Escape(old1)}");
    }
    else
    {
        Console.WriteLine($"旧(可见)  : {Escape(old1)}");
        Console.WriteLine($"新(可见)  : {Escape(new1)}");
        if (old2 != new2)
        {
            Console.WriteLine($"旧(隐藏)  : {Escape(old2)}");
            Console.WriteLine($"新(隐藏)  : {Escape(new2)}");
        }
    }

    Console.WriteLine();
}

Console.WriteLine($"===== 汇总: {samples.Count} 个样例, {mismatches} 个不一致 =====");

static string Escape(string s) => s.Replace("\n", "\\n").Replace("\r", "\\r");
