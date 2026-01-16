using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CC98.Controls.UbbRender;
// 表情规则配置
public class EmoticonRule
{
    public Regex Pattern { get; set; }
    public string UrlTemplate { get; set; }
    public string Name { get; set; } // 可选：表情组名称

    public EmoticonRule(string pattern, string urlTemplate, string name = null)
    {
        Pattern = new Regex($"^{pattern}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        UrlTemplate = urlTemplate;
        Name = name;
    }

    public bool IsMatch(string tagName) => Pattern.IsMatch(tagName);
    public string GetUrl(string tagName) => Pattern.Replace(tagName, UrlTemplate);
}

// 表情规则管理器
public static class EmoticonRules
{
    private static readonly List<EmoticonRule> _rules = new List<EmoticonRule>
    {
        // ac娘：ac + 2-4位数字
        new EmoticonRule(@"ac(\d{2,4})", "ms-appx:///Assets/Emoji/ac-white/ac$1.png", "ac娘"),
        
        // 经典表情：em + 2位数字
        new EmoticonRule(@"em(\d{2})", "ms-appx:///Assets/Emoji/em/em$1.gif", "经典表情"),
        
        // 贴吧/雀魂等：任意2字母 + 2位数字
        new EmoticonRule(@"([a-zA-Z]{2})(\d{2})", "ms-appx:///Assets/Emoji/$1/$1$2.png", "通用表情"),
        
        // CC98：cc98 + 2位数字
        new EmoticonRule(@"cc98(\d{2})", "ms-appx:///Assets/Emoji/CC98/CC98$1.png", "CC98表情")
    };

    public static bool IsEmoticonTag(string tagName)
    {
        if (string.IsNullOrEmpty(tagName))
            return false;

        // 检查是否匹配任何表情规则
        return _rules.Any(rule => rule.IsMatch(tagName));
    }

    public static string GetEmoticonUrl(string tagName)
    {
        if (string.IsNullOrEmpty(tagName))
            return null;

        // 按照规则顺序匹配，返回第一个匹配的URL
        foreach (var rule in _rules)
        {
            if (rule.IsMatch(tagName))
            {
                return rule.GetUrl(tagName);
            }
        }

        return null;
    }

    public static (string url, string groupName)? GetEmoticonInfo(string tagName)
    {
        if (string.IsNullOrEmpty(tagName))
            return null;

        foreach (var rule in _rules)
        {
            if (rule.IsMatch(tagName))
            {
                return (rule.GetUrl(tagName), rule.Name);
            }
        }

        return null;
    }

    // 获取所有支持的标签前缀（用于优化判断）
    public static HashSet<string> GetSupportedPrefixes()
    {
        var prefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rule in _rules)
        {
            // 从正则模式中提取可能的字符前缀
            var pattern = rule.Pattern.ToString().TrimStart('^');

            // 简单提取字母前缀（对于前三个字符）
            if (pattern.StartsWith(@"ac"))
                prefixes.Add("ac");
            else if (pattern.StartsWith(@"em"))
                prefixes.Add("em");
            else if (pattern.StartsWith(@"cc98"))
                prefixes.Add("cc98");
            else if (pattern.StartsWith(@"([a-zA-Z]{2})"))
            {
                prefixes.Add("tb"); // 贴吧
                prefixes.Add("ms"); // 雀魂
            }
        }

        return prefixes;
    }
}