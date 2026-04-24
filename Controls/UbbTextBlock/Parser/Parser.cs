using CC98.Controls.UbbTextBlock.Tokenizer;

namespace CC98.Controls.UbbTextBlock.Parser;


/// <summary>
/// 解析入口点
/// </summary>
public class Parser
{
    public static UbbDocument Parse(string ubbText)
    {
        // 词法分析
        var scanner = new UbbTokenizer(ubbText);
        var tokens = scanner.ScanTokens();
        // 语法分析
        var parser = new UbbParser(tokens);
        var document = parser.Parse();

        return document;
    }
}

