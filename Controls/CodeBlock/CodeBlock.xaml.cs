using System.Globalization;
using ColorCode;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Windows.ApplicationModel.DataTransfer;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98.Controls.CodeBlock;

public sealed partial class CodeBlock : UserControl
{
    public CodeBlock()
    {
        InitializeComponent();
        RenderCode();
    }

    #region 辅助函数

    private string GetLanguageDisplayName(ILanguage language, string languageName)
    {
        // 如果语言对象有 Name 属性，使用它
        if (!string.IsNullOrEmpty(language.Name))
            return language.Name;

        // 否则根据语言 ID 返回友好名称
        return languageName.ToLowerInvariant() switch
        {
            "cpp" => "C++",
            "csharp" or "cs" => "C#",
            "javascript" or "js" => "JavaScript",
            "typescript" or "ts" => "TypeScript",
            "python" or "py" => "Python",
            "java" => "Java",
            "html" => "HTML",
            "css" => "CSS",
            "sql" => "SQL",
            "php" => "PHP",
            "ruby" or "rb" => "Ruby",
            "go" => "Go",
            "rust" => "Rust",
            "swift" => "Swift",
            "kotlin" => "Kotlin",
            "dart" => "Dart",
            "markdown" or "md" => "Markdown",
            "yaml" => "YAML",
            "json" => "JSON",
            "xml" => "XML",
            "bash" or "sh" => "Bash",
            "powershell" or "ps" => "PowerShell",
            "plaintext" or "text" => "plaintext",
            _ => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(languageName)
        };
    }

    #endregion

    #region 依赖属性

    public static readonly DependencyProperty CodeProperty =
        DependencyProperty.Register("Code", typeof(string), typeof(CodeBlock),
            new(null, OnCodeChanged));


    public new static readonly DependencyProperty LanguageProperty =
        DependencyProperty.Register("Language", typeof(string), typeof(CodeBlock),
            new("plaintext"));

    public string Code
    {
        get => (string)GetValue(CodeProperty);
        set => SetValue(CodeProperty, value);
    }

    public string LanguageName
    {
        get => (string)GetValue(LanguageProperty);
        set => SetValue(LanguageProperty, value);
    }

    #endregion


    #region 初始化

    private void RenderCode()
    {
        var languageName = LanguageName;
        if (string.IsNullOrEmpty(languageName)) languageName = "plaintext";
        var displayName = string.Empty;
        var language = Languages.Cpp;
        if (languageName == "plaintext")
        {
            displayName = languageName;
        }
        else
        {
            language = Languages.FindById(languageName) ?? Languages.Cpp;
            displayName = GetLanguageDisplayName(language, languageName);
        }

        LanguageTag.Text = displayName;
        if (languageName != "plaintext")
        {
            var formatter = new RichTextBlockFormatter();
            formatter.FormatRichTextBlock(Code, language, Viewer);
        }
        else
        {
            Viewer.Blocks.Clear();
            var paragraph = new Paragraph();
            var run = new Run { Text = Code };
            paragraph.Inlines.Add(run);
            Viewer.Blocks.Add(paragraph);
        }
    }

    private static void OnCodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CodeBlock codeBlock) codeBlock.RenderCode();
    }

    #endregion

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        var package= new DataPackage();
        package.SetText(Code);
        Clipboard.SetContent(package);
    }
}