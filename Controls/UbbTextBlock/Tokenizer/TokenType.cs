namespace UbbRender.Tokenizer;
/// <summary>
/// 定义UBB语法的最小单元。
/// </summary>
public enum TokenType
{
    // 基础符号
    LeftBracket,    // [
    RightBracket,   // ]
    Slash,          // /
    Equal,          // =
    Comma,          // ,

    // 公式符号
    Dollar,         // $
    DoubleDollar,   // $$

    // 内容类
    TagName,        // b, url, img, ac01 等
    AttrValue,      // 属性值
    AutoLink,       // 自动识别的链接
    Text,           // 普通文本
    EOF             // 结束符
}