namespace CC98.Controls.UbbTextBlock.Tokenizer;

public class Token(TokenType type, string value, int position)
{
    public TokenType Type { get; set; } = type;
    public string Value { get; set; } = value;
    public int Position { get; set; } = position;
    public int Length => Value?.Length ?? 0;
}