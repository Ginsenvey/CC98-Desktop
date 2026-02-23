using ColorCode;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98.Share.Controls
{
    public sealed partial class CodeBlock : UserControl
    {
        public CodeBlock()
        {
            InitializeComponent();
            RenderCode();
        }

        #region 依赖属性
        public static readonly DependencyProperty CodeProperty =
        DependencyProperty.Register("Code", typeof(string), typeof(CodeBlock),
            new PropertyMetadata(null, OnCodeChanged));

        

        public static new readonly DependencyProperty LanguageProperty =
        DependencyProperty.Register("Language", typeof(string), typeof(CodeBlock),
            new PropertyMetadata("PlainText"));
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
            if (string.IsNullOrEmpty(languageName))
            {
                languageName = "PlainText";
            }
            string displayName = string.Empty;
            var language = ColorCode.Languages.Cpp;
            if (languageName == "PlainText")
            {
                displayName = languageName;
            }
            else
            {
                language = ColorCode.Languages.FindById(languageName) ?? ColorCode.Languages.Cpp;
                displayName = GetLanguageDisplayName(language, languageName);
            }
            LanguageTag.Text = displayName;
            if (languageName != "PlainText")
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
            if(d is CodeBlock codeBlock)
            {
                codeBlock.RenderCode();
            }
        }
        #endregion

        #region 辅助函数

        private string GetLanguageDisplayName(ColorCode.ILanguage language, string languageName)
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
                "plaintext" or "text" => "Plain Text",
                _ => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(languageName)
            };
        }

        #endregion
    }
}
