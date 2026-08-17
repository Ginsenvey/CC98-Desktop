using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using CC98.Kernel;
using CC98.Objects;
using CC98.Services;
using CommunityToolkit.WinUI.Controls;
using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Storage.Pickers;
using CC98.Services.Extensions;
using CC98.Services.Helpers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SketchPage : Page
{
    private string Tail => AppSettings.Current.LittleTail;

    private string TextContent = "";

    //约定:可以在本页更改的环境量由以下字段表示，
    //而不可变参数由NavigationInfo传入。
    private int TextContentType; //UBB
    private string CurrentLabel = ""; //记录实时指令
    private List<Emoji> Emojis = [];
    // 表情分组缓存:悬停切换类型时避免重复扫描磁盘与重建整组 Image
    private readonly Dictionary<string, List<Emoji>> _emojiCache = [];
    private string _currentEmojiType = "";
    private bool IsAnonymous = false;
    private bool IsTailVisible;
    private bool NotifyAllReplier = false;
    private bool NotifyPoster = true;
    private int PostTypeValue; //普通帖子
    private ApiService ApiService = App.Current.GetService<ApiService>();
    public SketchPage()
    {
        InitializeComponent();
        if (App.Current.AppMainWindow is MainWindow mainwindow) mainwindow.NavigationView.IsPaneOpen = false;
        LoadEmojiSet("CC98");
    }

    public SketchNavigationInfo NavigationInfo { get; set; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var args = e.TryGetParameter<SketchNavigationInfo>();
        if (args == null) return;
        NavigationInfo = args;
        ApplyEditorEnv();
    }

    #region 初始化环境

    private void LoadEmojiSet(string type)
    {
        //存在问题，如果使用xaml绑定,向下滚动时会崩溃。因此使用代码。
        //类型未变化(如悬停回当前类型)时直接跳过,避免重复扫描与重建
        if (_currentEmojiType == type && Emojis.Count > 0) return;
        _currentEmojiType = type;

        EmojiContainer.ItemsSource = null;
        Emojis.Clear();

        if (!_emojiCache.TryGetValue(type, out var cached))
        {
            try
            {
                var emojiPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Emoji", type);
                var files = Directory.GetFiles(emojiPath, "*", SearchOption.TopDirectoryOnly);
                cached = files.Select(file =>
                {
                    var filename = Path.GetFileName(file);
                    return new Emoji { EmojiName = filename.Split(".")[0].ToLower(), EmojiPath = file };
                }).ToList();
                _emojiCache[type] = cached;
            }
            catch (Exception ex)
            {
                // 目录缺失/被改名:避免悬停事件中抛未捕获异常崩溃
                Debug.WriteLine($"加载表情失败: {ex.Message}");
                cached = [];
            }
        }

        Emojis.AddRange(cached);
        EmojiContainer.ItemsSource = Emojis;
    }

    //将编辑器模式应用到UI
    private void ApplyEditorEnv()
    {
        IsTailVisible = AppSettings.Current.IsTailVisible;
        TextContentType = NavigationInfo.ContentType;
        if (TextContentType == (int)ContentType.Markdown)
        {
            MdViewer.Visibility = Visibility.Visible;
            UbbViewer.Visibility = Visibility.Collapsed;
        }

        switch (NavigationInfo.EditorMode)
        {
            case EditorMode.ReplyToTopic:
                status.Text = $"回复主题:{NavigationInfo.HintText}";
                replyselector.IsSelected = true;
                //回复主题不需要设置标题和帖子类型
                SetTitle.IsEnabled = false;
                SetPostType.IsEnabled = false;
                break;
            case EditorMode.ReplyToPost:
                status.Text = NavigationInfo.HintText;
                Editor.Text = NavigationInfo.QuoteHeader;
                if (!Editor.Text.EndsWith(Environment.NewLine)) Editor.Text += Environment.NewLine;
                Editor.SelectionStart = Editor.Text.Length;
                replyselector.IsSelected = true;
                SetTitle.IsEnabled = false;
                SetPostType.IsEnabled = false;
                break;
            case EditorMode.DraftNewTopic:
                status.Text = "发表新主题";
                SetTitle.IsEnabled = true;
                topicselector.IsSelected = true;
                break;
            case EditorMode.EditMyPost:
                status.Text = $"编辑帖子:{NavigationInfo.HintText}";
                Editor.Text = NavigationInfo.BaseText;
                SetTitle.Text = NavigationInfo.HintText;
                if (!Editor.Text.EndsWith(Environment.NewLine)) Editor.Text += Environment.NewLine;
                Editor.SelectionStart = Editor.Text.Length;
                replyselector.IsSelected = true;
                //编辑非主题帖不允许修改标题和帖子类型
                SetTitle.IsEnabled = false;
                SetPostType.IsEnabled = false;
                break;
            case EditorMode.EditMyTopic:
                status.Text = $"编辑主题:{NavigationInfo.HintText}";
                Editor.Text = NavigationInfo.BaseText;
                SetTitle.Text = NavigationInfo.HintText;
                if (!Editor.Text.EndsWith(Environment.NewLine)) Editor.Text += Environment.NewLine;
                Editor.SelectionStart = Editor.Text.Length;
                topicselector.IsSelected = true;
                SetTitle.IsEnabled = true;
                SetPostType.IsEnabled = false;
                break;
        }

        //初始化内容,以免由于xaml加载顺序content为空。有时候textchanged事件不会立即触发。
        TextContent = Editor.Text.Replace("\r\n", "\n").Replace("\r", "\n");
        ApplyContentToViewer();
    }

    //渲染实时预览
    private void ApplyContentToViewer()
    {
        //关闭预览窗格可以避免卡顿，尤其是在内容较长时
        if (!EditArea.IsPaneOpen) return;
        if (TextContentType == (int)Objects.ContentType.Ubb)
            UbbViewer.UbbText = TextContent;
        else
            MdViewer.Text = TextContent;
    }

    #endregion


    #region 编辑器

    private async void AppBarButton_Click(object sender, RoutedEventArgs e)
    {
        var b = sender as AppBarButton;
        if (b == null) return;
        var isMd = TextContentType == (int)Objects.ContentType.Markdown;
        switch (b.Label)
        {
            case "预览":
                EditArea.IsPaneOpen = true;
                break;
            case "粗体":
                if (isMd) InsertMdSyntax("**", "**", "粗体文本");
                else InsertTag("b", "b", "");
                break;
            case "斜体":
                if (isMd) InsertMdSyntax("*", "*", "斜体文本");
                else InsertTag("i", "i", "");
                break;
            case "删除线":
                if (isMd) InsertMdSyntax("~~", "~~", "删除线文本");
                else InsertTag("del", "del", "");
                break;
            case "下划线":
                if (isMd) InsertMdSyntax("<u>", "</u>", "下划线文本");
                else InsertTag("u", "u", "");
                break;
            case "左对齐":
                if (isMd) InsertMdSyntax("<div align=\"left\">\n", "\n</div>", "文本", isBlock: true);
                else InsertTag("align=left", "align", "");
                break;
            case "居中":
                if (isMd) InsertMdSyntax("<div align=\"center\">\n", "\n</div>", "文本", isBlock: true);
                else InsertTag("align=center", "align", "");
                break;
            case "右对齐":
                if (isMd) InsertMdSyntax("<div align=\"right\">\n", "\n</div>", "文本", isBlock: true);
                else InsertTag("align=right", "align", "");
                break;
            case "引用":
                if (isMd) InsertMdQuote();
                else InsertTag("quote", "quote", "");
                break;
            case "代码":
                if (isMd) InsertMdSyntax("```\n", "\n```", "code", isBlock: true);
                else InsertTag("code", "code", "");
                break;
            case "链接":
                if (isMd) InsertMdLink();
                else InsertTag("url", "url", "");
                break;
            case "颜色":
                var r = await ShowDialogSafelyAsync(ColorPanel);
                if (r == ContentDialogResult.Primary)
                {
                    var colorwithalpha = Colors.Color.ToString().ToLower();
                    var color = string.Concat(colorwithalpha.AsSpan(0, 1), colorwithalpha.AsSpan(3, 6));
                    if (isMd) InsertMdSyntax($"<span style=\"color:#{color}\">", "</span>", "彩色文本");
                    else InsertTag("color=" + color, "color", "");
                }

                break;
            case "图片":
                CurrentLabel = "img";
                FileHelper.XamlRoot = XamlRoot;
                await ShowDialogSafelyAsync(FileHelper);
                break;
            case "视频":
                CurrentLabel = "video";
                FileHelper.XamlRoot = XamlRoot;
                await ShowDialogSafelyAsync(FileHelper);
                break;
            case "音频":
                CurrentLabel = "audio";
                FileHelper.XamlRoot = XamlRoot;
                await ShowDialogSafelyAsync(FileHelper);
                break;
            case "哔哩":
                // UBB 格式 [bili]BV号[/bili],中间是 BV 号
                if (isMd) InsertMdSyntax("<iframe src=\"//player.bilibili.com/player.html?bvid=", "\" scrolling=\"no\" border=\"0\" frameborder=\"no\" framespacing=\"0\" allowfullscreen=\"true\"></iframe>", "BV1xx411c7mD");
                else InsertTag("bili", "bili", "");
                break;
            case "文档":
                CurrentLabel = "upload";
                FileHelper.XamlRoot = XamlRoot;
                await ShowDialogSafelyAsync(FileHelper);
                break;
            case "分割线":
                if (isMd)
                {
                    var mdSelStart = Editor.SelectionStart;
                    Editor.Text = Editor.Text.Insert(mdSelStart, "\n---\n");
                    Editor.SelectionStart = mdSelStart + 4;
                    Editor.Focus(FocusState.Programmatic);
                }
                else
                {
                    var selectionStart = Editor.SelectionStart;
                    Editor.Text = Editor.Text.Insert(selectionStart, "[line]");
                    Editor.SelectionStart = selectionStart + 6;
                    Editor.Focus(FocusState.Programmatic);
                }
                break;
            case "贴图":
                // TODO: 实现贴图功能
                break;
        }
    }

    /// <summary>
    /// MD 模式插入引用:选中文本每行加 "> ",无选中时插入占位。
    /// </summary>
    private void InsertMdQuote()
    {
        var selStart = Editor.SelectionStart;
        var selLen = Editor.SelectionLength;
        if (selLen > 0)
        {
            var selected = Editor.Text.Substring(selStart, selLen);
            var quoted = "> " + selected.Replace("\n", "\n> ");
            Editor.Text = Editor.Text.Remove(selStart, selLen).Insert(selStart, quoted);
        }
        else
        {
            Editor.Text = Editor.Text.Insert(selStart, "\n> 引用文本\n");
            Editor.SelectionStart = selStart + 3;
        }
        Editor.Focus(FocusState.Programmatic);
    }

    /// <summary>
    /// MD 模式插入链接:插入 [链接文字](url),选中"链接文字"供直接输入。
    /// </summary>
    private void InsertMdLink()
    {
        const string placeholder = "链接文字";
        const string urlPlaceholder = "url";
        var md = $"[{placeholder}]({urlPlaceholder})";
        var selStart = Editor.SelectionStart;
        Editor.Text = Editor.Text.Insert(selStart, md);
        Editor.SelectionStart = selStart + 1; // 选中"链接文字"
        Editor.SelectionLength = placeholder.Length;
        Editor.Focus(FocusState.Programmatic);
    }

    private void InsertTag(string leftTag, string rightTag, string content, int offset = 0, bool selectText = false)
    {
        var openTag = $"[{leftTag}]";
        var closeTag = $"[/{rightTag}]";
        var fullTag = $"{openTag}{content}{closeTag}";
        var selectionStart = Editor.SelectionStart;
        Editor.Text = Editor.Text.Insert(selectionStart, fullTag);
        Editor.SelectionStart = selectionStart + openTag.Length + content.Length + offset;
        if (selectText)
            Editor.SelectionLength = content.Length;
        else
            Editor.SelectionLength = 0;
        Editor.Focus(FocusState.Programmatic);
    }

    /// <summary>
    /// 在光标位置插入 MD 包裹语法。
    /// </summary>
    /// <param name="left">左侧包裹符号</param>
    /// <param name="right">右侧包裹符号</param>
    /// <param name="placeholder">无选中文本时的占位文字,插入后自动选中,输入即可替换</param>
    /// <param name="isBlock">块级语法时前后加换行</param>
    private void InsertMdSyntax(string left, string right, string placeholder = "", bool isBlock = false)
    {
        var selStart = Editor.SelectionStart;
        var selLen = Editor.SelectionLength;
        var hasSelection = selLen > 0;
        var selected = hasSelection ? Editor.Text.Substring(selStart, selLen) : placeholder;

        var prefix = isBlock ? "\n" : "";
        var suffix = isBlock ? "\n" : "";
        var fullTag = $"{prefix}{left}{selected}{right}{suffix}";

        Editor.Text = Editor.Text.Insert(selStart, fullTag);
        if (hasSelection)
        {
            // 选中文本被包裹,光标置于包裹内容之后
            Editor.SelectionStart = selStart + prefix.Length + left.Length + selected.Length;
            Editor.SelectionLength = 0;
        }
        else if (!string.IsNullOrEmpty(placeholder))
        {
            // 选中占位文字,输入即可替换
            Editor.SelectionStart = selStart + prefix.Length + left.Length;
            Editor.SelectionLength = placeholder.Length;
        }
        else
        {
            Editor.SelectionStart = selStart + prefix.Length + left.Length;
            Editor.SelectionLength = 0;
        }
        Editor.Focus(FocusState.Programmatic);
    }

    /// <summary>
    /// 在 MD 模式下插入媒体标签（图片/视频/音频/文档）。
    /// </summary>
    private void InsertMdMedia(string label, string url)
    {
        var md = label switch
        {
            "img" => $"![图片]({url})",
            "video" => $@"<video src=""{url}"" controls></video>",
            "audio" => $@"<audio src=""{url}"" controls></audio>",
            "upload" => $"[下载]({url})",
            _ => url
        };
        var selStart = Editor.SelectionStart;
        Editor.Text = Editor.Text.Insert(selStart, md);
        Editor.SelectionStart = selStart + md.Length;
        Editor.Focus(FocusState.Programmatic);
    }

    private void Editor_TextChanged(object sender, TextChangedEventArgs e)
    {
        TextContent = Editor.Text.Replace("\r\n", "\n").Replace("\r", "\n");
        // 防抖:连续输入期间 300ms 后才刷新一次预览,避免每次击键全量重渲染
        _previewDebounceTimer ??= CreatePreviewDebounceTimer();
        _previewDebounceTimer.Stop();
        _previewDebounceTimer.Start();
    }

    // 预览刷新防抖计时器
    private DispatcherTimer? _previewDebounceTimer;

    private DispatcherTimer CreatePreviewDebounceTimer()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            ApplyContentToViewer();
        };
        return timer;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        // 停止防抖计时器,避免计时器持有页面引用造成泄漏
        _previewDebounceTimer?.Stop();
        base.OnNavigatedFrom(e);
    }

    //以下方法用于创建Md的代码块,但是UBB编辑器不需要支持这个操作。
    private void InsertCodeBlock()
    {
        var cursorPos = Editor.SelectionStart;
        var selectionLen = Editor.SelectionLength;
        var originalText = Editor.Text;
        var textWithoutSelection = originalText.Remove(cursorPos, selectionLen);
        var codeBlock = "`" + "`";
        var newText = textWithoutSelection.Insert(cursorPos, codeBlock);
        Editor.Text = newText;
        var newCursorPos = cursorPos + 1;
        Editor.SelectionStart = newCursorPos;
        Editor.Focus(FocusState.Programmatic);
    }

    private void EmojiContainer_ItemClick(object sender, ItemClickEventArgs e)
    {
        var i = e.ClickedItem;
        if (i == null) return;
        var index = EmojiContainer.Items.IndexOf(i);
        if (Emojis == null) return;
        if (Emojis.Count <= index) return;
        var tag = Emojis[index].EmojiName;
        if (tag == null) return;
        var selectionStart = Editor.SelectionStart;
        var emojiName = "[" + tag + "]";
        Editor.Text = Editor.Text.Insert(selectionStart, emojiName);
        Editor.SelectionStart = selectionStart + emojiName.Length;
        Editor.Focus(FocusState.Programmatic);
    }

    private async void FileUploadChoice_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FileUploadChoice.SelectedIndex != -1)
        {
            var item = FileUploadChoice.SelectedItem as ListViewItem;
            if (item == null) return;
            var tag = item?.Tag;
            if (tag == null) return;
            var operation = tag.ToInt();
            if (operation == 1)
            {
                // 上传本地文件:选择后上传,成功后按当前模式插入标签
                FileHelper.Hide();
                var result = await FileUploadService.PickAndUploadAsync(XamlRoot, CurrentLabel);
                if (!result.Success)
                {
                    if (!string.IsNullOrEmpty(result.Error))
                        Flower.Play(FlowStatus.Fail, $"上传失败:{result.Error}");
                    return;
                }

                if (TextContentType == (int)Objects.ContentType.Markdown)
                    InsertMdMedia(CurrentLabel, result.Url);
                else
                    InsertTag(CurrentLabel, CurrentLabel, result.Url);
            }
            else if (operation == 2)
            {
                // 仅输入标签:插入空标签,由用户在编辑器中补充内容
                FileHelper.Hide();
                if (TextContentType == (int)Objects.ContentType.Markdown)
                    InsertMdMedia(CurrentLabel, "");
                else
                    InsertTag(CurrentLabel, CurrentLabel, "");
            }
            else
            {
                // 使用自定义 URL
                CustomLink.Visibility = Visibility.Visible;
                CustomLink.Focus(FocusState.Keyboard); //自动聚焦，减少鼠标操作
            }
        }
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var r = await SendDialog.ShowAsync();
            if (r != ContentDialogResult.Primary) return;
            if (string.IsNullOrWhiteSpace(TextContent))
            {
                Flower.Play(FlowStatus.Warning, "内容不能为空");
                return;
            }
            switch (NavigationInfo.EditorMode)
            {
                case EditorMode.ReplyToTopic:
                case EditorMode.ReplyToPost:
                    await SendReply();
                    break;
                case EditorMode.EditMyPost:
                case EditorMode.EditMyTopic:
                    await EditPost();
                    break;
                case EditorMode.DraftNewTopic:
                    await DraftNewTopic();
                    break;
            }
        }
        catch (Exception ex)
        {
            // 对话框宿主卸载/网络异常等:避免 async void 未捕获异常导致进程崩溃
            Debug.WriteLine($"发送失败: {ex.Message}");
            Flower.Play(FlowStatus.Fail, "发送失败，请重试");
        }
    }

    /// <summary>
    /// 安全地显示对话框:页面被导航移除等场景下 ShowAsync 会抛异常,在此兜底。
    /// </summary>
    private async Task<ContentDialogResult> ShowDialogSafelyAsync(ContentDialog dialog)
    {
        try
        {
            return await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"对话框打开失败: {ex.Message}");
            return ContentDialogResult.None;
        }
    }

    #endregion

    #region 实现请求

    private async Task EditPost()
    {
        var url = ApiEndpoints.Post.Edit(NavigationInfo.PostId);
        var request = new EditPostRequest
        {
            Content = TextContent,
            ContentType = TextContentType,
            NotifyPoster = NotifyPoster,
            Title = SetTitle.Text
        };
        var text = SerializationHelper.TrySerialize(request);
        var requestBody = new StringContent(text, Encoding.UTF8, "application/json");
        var res = await ApiService.Put(url, requestBody);
        if (!res.IsSuccess)
        {
            status.Text = $"编辑失败:{res.Message}";
        }
        else
        {
            var param = new TopicNavigationInfo
            {
                IsJumpingMode = true,
                TopicId = NavigationInfo.TopicId,
                TargetFloor = NavigationInfo.Floor
            };
            GlobalService.Instance.NavigationAnchor = param;
            GoBack();
        }
    }

    private async Task SendReply()
    {
        var url = ApiEndpoints.Topic.SendReply(NavigationInfo.TopicId);
        // 尾注只在发送时追加,不修改 TextContent,避免重试时重复追加
        var content = IsTailVisible ? TextContent + "\n" + Tail : TextContent;
        // 服务器仅在 ReplyToPost 模式接受 parentId 字段,否则 500,故按模式使用不同请求类型
        string text;
        if (NavigationInfo.EditorMode == EditorMode.ReplyToPost)
        {
            text = SerializationHelper.TrySerialize(new SendReplyToPostRequest
            {
                Content = content,
                ContentType = TextContentType,
                IsAnonymous = IsAnonymous,
                NotifyAllReplier = NotifyAllReplier,
                Title = "",
                ParentId = NavigationInfo.ParentId
            });
        }
        else
        {
            text = SerializationHelper.TrySerialize(new SendReplyRequest
            {
                Content = content,
                ContentType = TextContentType,
                IsAnonymous = IsAnonymous,
                NotifyAllReplier = NotifyAllReplier,
                Title = ""
            });
        }

        var requestBody = new StringContent(text, Encoding.UTF8, "application/json");
        var res = await ApiService.Submit<int>(url, requestBody);
        if (!res.IsSuccess)
        {
            //
            status.Text = $"发送失败:{res.Message}";
        }
        else
        {
            var param = new TopicNavigationInfo
            {
                IsJumpingMode = NavigationInfo.EditorMode == EditorMode.ReplyToPost,
                TopicId = NavigationInfo.TopicId,
                TargetFloor = NavigationInfo.Floor,
                GoToLatest = NavigationInfo.EditorMode == EditorMode.ReplyToTopic
            };
            GlobalService.Instance.NavigationAnchor = param;
            GoBack();
        }
    }

    private async Task DraftNewTopic()
    {
        var url = ApiEndpoints.Board.SendNewTopic(NavigationInfo.BoardId);
        var request = new CreateTopicRequest
        {
            Content = TextContent,
            ContentType = TextContentType,
            IsAnonymous = IsAnonymous,
            NotifyPoster = NotifyPoster,
            Title = SetTitle.Text,
            Type = PostTypeValue
        };
        var text = SerializationHelper.TrySerialize(request);
        var requestBody = new StringContent(text, Encoding.UTF8, "application/json");
        var res = await ApiService.Submit<int>(url, requestBody);
        if (!res.IsSuccess)
        {
            //
            status.Text = $"发送新主题失败:{res.Message}";
        }
        else
        {
            var newTopicId = res.Data;
            var param = new TopicNavigationInfo
            {
                IsJumpingMode = false,
                TopicId = newTopicId
            };
            GlobalService.Instance.NavigationAnchor = param;
            GoBack();
        }
    }

    #endregion


    #region UI事件处理

    private void PreviewMode_Click(object sender, RoutedEventArgs e)
    {
        EditArea.IsPaneOpen = !EditArea.IsPaneOpen;
        if (EditArea.IsPaneOpen) ApplyContentToViewer();
    }

    private void EmojiType_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var type = sender as SegmentedItem;
        if (type == null) return;
        type.IsSelected = true;
        if (type.Tag is not string tag) return;
        LoadEmojiSet(tag);
    }

    private void SwitchContentType_Click(object sender, RoutedEventArgs e)
    {
        if (TextContentType == (int)ContentType.Ubb)
        {
            TextContentType = 1;
            MD.Visibility = Visibility.Visible;
            UBB.Visibility = Visibility.Collapsed;
            MdViewer.Visibility = Visibility.Visible;
            UbbViewer.Visibility = Visibility.Collapsed;
            Flower.Play("\uE946", "切换到Markdown");
        }
        else
        {
            TextContentType = 0;
            MD.Visibility = Visibility.Collapsed;
            UBB.Visibility = Visibility.Visible;
            MdViewer.Visibility = Visibility.Collapsed;
            UbbViewer.Visibility = Visibility.Visible;
            Flower.Play("\uE946", "切换到UBB");
        }

        ApplyContentToViewer();
    }

    private void ReceiveNotice_Unchecked(object sender, RoutedEventArgs e)
    {
        NotifyPoster = false;
    }

    private void ReceiveNotice_Checked(object sender, RoutedEventArgs e)
    {
        NotifyPoster = true;
    }

    private void PostType_Checked(object sender, RoutedEventArgs e)
    {
        var r = sender as RadioButton;
        if (r?.Tag is not int type) return;
        PostTypeValue = type;
    }

    private void ConfirmCustomLink_Click(object sender, RoutedEventArgs e)
    {
        if (TextContentType == (int)Objects.ContentType.Markdown)
            InsertMdMedia(CurrentLabel, CustomLink.Text);
        else
            InsertTag(CurrentLabel, CurrentLabel, CustomLink.Text);
        FileHelper.Hide();
    }

    private void FileHelper_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
    {
        CustomLink.Visibility = Visibility.Collapsed;
        FileUploadChoice.SelectedIndex = -1;
    }

    private void GoBack()
    {
        if (Frame.CanGoBack) Frame.GoBack();
    }

    #endregion
}