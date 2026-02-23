using System.Collections.Generic;

namespace UbbRender.Tokenizer;
/// <summary>
/// 将原始文本分解为一系列 Token 的词法分析器。
/// </summary>
/// <param name="input"></param>
public class UBBTokenizer(string input)
{
    private readonly string _input = input ?? "";
    private int _pos = 0;
    private bool _inTag = false;
    private TokenType _lastType = TokenType.EOF; // 记录上一个 Token 类型以判断上下文

    public IEnumerable<Token> ScanTokens()
    {
        while (_pos < _input.Length)
        {
            Token token;
            if (_inTag)
            {
                token = ScanInTag();
            }
            else
            {
                char c = Peek();
                if (c == '[')
                {
                    _inTag = true;
                    token = new Token(TokenType.LeftBracket, Advance().ToString(), _pos - 1);
                }
                else if (c == '$')
                {
                    token = ScanMathDelimiter();
                }
                else if (c == '\n' || c == '\r')
                {
                    token = ScanEnter();
                }
                else if (IsAtAutoLinkStart())
                {
                    token = ScanAutoLink();
                }
                else
                {
                    token = ScanText();
                }
            }

            _lastType = token.Type;
            yield return token;
        }

        yield return new Token(TokenType.EOF, "", _pos);
    }

    private Token ScanInTag()
    {
        char c = Peek();

        switch (c)
        {
            case ']':
                _inTag = false;
                return new Token(TokenType.RightBracket, Advance().ToString(), _pos - 1);
            case ',':
                return new Token(TokenType.Comma, Advance().ToString(), _pos - 1);
            case '=':
                // 关键修复：只有在 TagName 之后，等号才是属性开始的分隔符
                if (_lastType == TokenType.TagName)
                {
                    return new Token(TokenType.Equal, Advance().ToString(), _pos - 1);
                }
                // 否则（例如在 URL 内部），它是属性内容的一部分，交给 ScanTagContent 处理
                return ScanTagContent();
            case '/':
                if (_lastType == TokenType.LeftBracket)
                {
                    return new Token(TokenType.Slash, Advance().ToString(), _pos - 1);
                }
                return ScanTagContent();
            default:
                return ScanTagContent();
        }
    }



    private Token ScanTagContent()
    {
        int start = _pos;
        while (_pos < _input.Length && !IsTagDelimiter(Peek()))
        {
            Advance();
        }

        string value = _input[start.._pos];

        // 核心逻辑：根据上一个 Token 判断当前内容的性质
        // 如果前面是 '[' 或 '[/'，则当前是标签名
        if (_lastType == TokenType.LeftBracket || _lastType == TokenType.Slash)
        {
            return new Token(TokenType.TagName, value, start);
        }

        // 否则（前面是 '=' 或 ','），视为属性值
        return new Token(TokenType.AttrValue, value, start);
    }

    private Token ScanText()
    {
        int start = _pos;
        while (_pos < _input.Length && Peek() != '[' && Peek() != '$' && Peek() != '\n' && Peek() != '\r')
        {
            Advance();
            // 如果在文本中遇到自动链接的起始位置，则停止以便让 ScanAutoLink 处理它
            if (IsAtAutoLinkStart()) break;
        }
        return new Token(TokenType.Text, _input[start.._pos], start);
    }
    // 新增：专门处理换行符的方法
    private Token ScanEnter()
    {
        int start = _pos;
        char c = Peek();

        // 处理 \r\n 组合（Windows风格换行）
        if (c == '\r' && _pos + 1 < _input.Length && _input[_pos + 1] == '\n')
        {
            Advance(); // 消费 \r
            Advance(); // 消费 \n
            return new Token(TokenType.Enter, "\r\n", start);
        }

        // 处理单独的 \n 或 \r
        Advance();
        return new Token(TokenType.Enter, c.ToString(), start);
    }
    private Token ScanMathDelimiter()
    {
        int start = _pos;
        Advance();
        if (Peek() == '$')
        {
            Advance();
            return new Token(TokenType.DoubleDollar, "$$", start);
        }
        return new Token(TokenType.Dollar, "$", start);
    }

    // 新增：判断当前位置是否为自动链接的起始位置
    private bool IsAtAutoLinkStart()
    {
        // 支持 http://, https://, www.
        if (_pos >= _input.Length) return false;
        int remaining = _input.Length - _pos;
        if (remaining >= 7 && _input[_pos..].StartsWith("http://")) return true;
        if (remaining >= 8 && _input[_pos..].StartsWith("https://")) return true;
        if (remaining >= 4 && _input[_pos..].StartsWith("www.")) return true;
        return false;
    }

    // 新增：扫描自动链接内容，直到遇到分隔符
    private Token ScanAutoLink()
    {
        int start = _pos;
        while (_pos < _input.Length)
        {
            char c = Peek();
            // 空白、行终止、UBB 起始符号认为是链接的终止
            if (char.IsWhiteSpace(c) || c == '\n' || c == '\r' || c == '[' || c == ']') break;
            Advance();
        }

        string value = _input[start.._pos];
        return new Token(TokenType.AutoLink, value, start);
    }

    private char Peek() => _pos < _input.Length ? _input[_pos] : '\0';
    private char Advance() => _input[_pos++];

    // 修改判定逻辑：= 是否作为分隔符取决于当前上下文
    private bool IsTagDelimiter(char c)
    {
        // ] , 和 结束符 永远是分隔符
        if (c == ']' || c == ',' || c == '\0') return true;

        // 只有在寻找 TagName 的阶段，= 才是分隔符
        if (c == '=' && (_lastType == TokenType.LeftBracket || _lastType == TokenType.Slash))
        {
            return true;
        }

        return false;
    }
}