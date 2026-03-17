using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using UbbRender.Parser;
using UbbRender.Tokenizer;

namespace UbbRender.Common;


/// <summary>
/// 解析入口点
/// </summary>
public class Parser
{
    public static UbbDocument Parse(string ubbText)
    {
        // 词法分析
        var scanner = new UBBTokenizer(ubbText);
        var tokens = scanner.ScanTokens();
        // 语法分析
        var parser = new UBBParser(tokens);
        var document = parser.Parse();

        return document;
    }
}

