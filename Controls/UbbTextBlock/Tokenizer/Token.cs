using System.Collections.Generic;

namespace UbbRender.Tokenizer;

public class Token
{
    public TokenType Type { get; set; }
    public string Value { get; set; }
    public int Position { get; set; }
    public int Length => Value?.Length ?? 0;

    public Token(TokenType type, string value, int position)
    {
        Type = type;
        Value = value;
        Position = position;
    }
}


