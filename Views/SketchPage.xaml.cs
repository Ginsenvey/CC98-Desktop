using System;
using System.Collections.Generic;
using System.IO;
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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SketchPage : Page
{
    public const string Tail =
        "[align=right][size=3][color=gray]——来自「[b][color=purple]CC98 For Windows[/color][/b]」[/color][/size][/align]";

    public string Content = "";

    //约定:可以在本页更改的环境量由以下字段表示，
    //而不可变参数由NavigationInfo传入。
    public int ContentType; //UBB
    public string CurrentLabel = ""; //记录实时指令
    public List<Emoji> Emojis = [];
    public bool IsAnonymous = false;
    public bool IsTailVisible;
    public bool NotifyAllReplier = false;
    public bool NotifyPoster = true;
    public int PostTypeValue; //普通帖子
    public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
    public ApiService ApiService = App.Current.GetService<ApiService>();
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
        EmojiContainer.ItemsSource = null;
        Emojis.Clear();
        var emojiPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Emoji", type);
        var files = Directory.GetFiles(emojiPath, "*", SearchOption.TopDirectoryOnly);
        foreach (var file in files)
        {
            var filename = Path.GetFileName(file);
            Emojis.Add(new() { EmojiName = filename.Split(".")[0].ToLower(), EmojiPath = file });
        }

        EmojiContainer.ItemsSource = Emojis;
    }

    //将编辑器模式应用到UI
    private void ApplyEditorEnv()
    {
        if (ValidationHelper.GetValue(Set, "IsTailVisible") == "1") IsTailVisible = true;
        ContentType = NavigationInfo.ContentType;
        if (ContentType == (int)Objects.ContentType.Markdown)
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
        Content = Editor.Text.Replace("\r\n", "\n").Replace("\r", "\n");
        ApplyContentToViewer();
    }

    //渲染实时预览
    private void ApplyContentToViewer()
    {
        //关闭预览窗格可以避免卡顿，尤其是在内容较长时
        if (!EditArea.IsPaneOpen) return;
        if (ContentType == (int)Objects.ContentType.Ubb)
            UbbViewer.UbbText = Content;
        else
            MdViewer.Text = Content;
    }

    #endregion


    #region 编辑器

    private async void AppBarButton_Click(object sender, RoutedEventArgs e)
    {
        if (ContentType == (int)Objects.ContentType.Markdown)
        {
            Flower.Play(FlowStatus.Info, "当前处于Markdown模式下");
            return;
        }

        var b = sender as AppBarButton;
        if (b == null) return;
        switch (b.Label)
        {
            case "预览":
                EditArea.IsPaneOpen = true;
                break;
            case "粗体":
                InsertTag("b", "b", "");
                break;
            case "斜体":
                InsertTag("i", "i", "");
                break;
            case "删除线":
                InsertTag("del", "del", "");
                break;
            case "下划线":
                InsertTag("u", "u", "");
                break;
            case "左对齐":
                InsertTag("align=left", "align", "");
                break;
            case "居中":
                InsertTag("align=center", "align", "");
                break;
            case "右对齐":
                InsertTag("align=right", "align", "");
                break;
            case "引用":
                InsertTag("quote", "quote", "");
                break;
            case "代码":
                InsertTag("code", "code", "");
                break;
            case "链接":
                InsertTag("url", "url", "");
                break;
            case "颜色":
                var r = await ColorPanel.ShowAsync();
                if (r == ContentDialogResult.Primary)
                {
                    var colorwithalpha = Colors.Color.ToString().ToLower();
                    var color = string.Concat(colorwithalpha.AsSpan(0, 1), colorwithalpha.AsSpan(3, 6));
                    InsertTag("color=" + color, "color", "");
                }

                break;
            case "图片":
                CurrentLabel = "img";
                FileHelper.XamlRoot = XamlRoot;
                await FileHelper.ShowAsync();
                break;
            case "视频":
                CurrentLabel = "video";
                FileHelper.XamlRoot = XamlRoot;
                await FileHelper.ShowAsync();
                break;
            case "音频":
                CurrentLabel = "audio";
                FileHelper.XamlRoot = XamlRoot;
                await FileHelper.ShowAsync();
                break;
            case "哔哩":
                InsertTag("bili", "bili", "");
                break;
            case "文档":
                CurrentLabel = "upload";
                FileHelper.XamlRoot = XamlRoot;
                await FileHelper.ShowAsync();
                break;
            case "分割线":
                var selectionStart = Editor.SelectionStart;
                Editor.Text = Editor.Text.Insert(selectionStart, "[line]");
                Editor.SelectionStart = selectionStart + 6;
                Editor.Focus(FocusState.Programmatic);
                break;
            case "贴图":

                break;
        }
    }

    private void InsertTag(string ltag, string rtag, string input, int offset = 0, bool select = true)
    {
        var openTag = $"[{ltag}]";
        var closeTag = $"[/{rtag}]";
        var fullTag = $"{openTag}{input}{closeTag}";
        var selectionStart = Editor.SelectionStart;
        Editor.Text = Editor.Text.Insert(selectionStart, fullTag);
        Editor.SelectionStart = selectionStart + openTag.Length + input.Length + offset;
        if (select)
            Editor.SelectionLength = input.Length;
        else
            Editor.SelectionLength = 0;
        Editor.Focus(FocusState.Programmatic);
    }

    private void Editor_TextChanged(object sender, TextChangedEventArgs e)
    {
        Content = Editor.Text.Replace("\r\n", "\n").Replace("\r", "\n");
        ApplyContentToViewer();
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
                var url = "";
                var filter = GetSuffixs(CurrentLabel);
                switch (CurrentLabel)
                {
                    case "img":
                        url = await PickAndUploadFile(filter, PickerLocationId.PicturesLibrary);
                        break;
                    case "video":
                        url = await PickAndUploadFile(filter, PickerLocationId.VideosLibrary);
                        break;
                    case "audio":
                        url = await PickAndUploadFile(filter, PickerLocationId.MusicLibrary);
                        break;
                }

                FileHelper.Hide();
                if (url != "0")
                    InsertTag(CurrentLabel, CurrentLabel, url);
                else
                    Flower.Play(FlowStatus.Info, "未上传文件");
            }
            else if (operation == 2)
            {
                FileHelper.Hide();
                InsertTag(CurrentLabel, CurrentLabel, "");
            }
            else
            {
                CustomLink.Visibility = Visibility.Visible;
                CustomLink.Focus(FocusState.Keyboard); //自动聚焦，减少鼠标操作
            }
        }
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        var r = await SendDialog.ShowAsync();
        if (r != ContentDialogResult.Primary) return;
        switch (NavigationInfo.EditorMode)
        {
            case EditorMode.ReplyToTopic:
                await SendReply();
                break;
            case EditorMode.ReplyToPost:
                await SendReply();
                break;
            case EditorMode.EditMyPost:
                await EditPost();
                break;
            case EditorMode.EditMyTopic:
                await EditPost();
                break;
            case EditorMode.DraftNewTopic:
                await DraftNewTopic();
                break;
        }
    }

    #endregion

    #region 实现请求

    private async Task EditPost()
    {
        var url = ApiEndpoints.Post.Edit(NavigationInfo.PostId);
        var reply = new Dictionary<string, object>
        {
            { "type", 0 },
            { "content", Content },
            { "contentType", ContentType },
            { "notifyPoster", NotifyPoster }, //常为true
            { "title", SetTitle.Text }
        };
        var replyText = SerializationHelper.TrySerialize(reply);
        var requestBody = new StringContent(replyText, Encoding.UTF8, "application/json");
        var res = await ApiService.Put(url, requestBody);
        if (!res.IsSuccess)
        {
            status.Text = $"编辑失败:{res.Message}";
            await App.Logger.WriteAsync("UBBEditor", "编辑帖子出错", res.Message);
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

    private async Task<string> UploadFileAsync(string filePath)
    {
        var url = ApiEndpoints.Forum.UploadFile;
        using var formData = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
        fileContent.Headers.ContentType = new("multipart/form-data");
        formData.Add(fileContent, "files", Path.GetFileName(filePath));
        var res = await ApiService.Submit<List<string>>(url, formData);
        if (!res.IsSuccess || res.Data == null)
        {
            //
            status.Text = $"上传失败:{res.Message}";
            await App.Logger.WriteAsync("UBBEditor", "上传文件失败", res.Message);
            return "";
        }

        var data = res.Data;
        if (data.Count > 0) return data[0];

        await App.Logger.WriteAsync("UBBEditor", "上传文件出错", "服务器未返回文件地址");
        return "";
    }

    private static List<string> GetSuffixs(string type)
    {
        return type switch
        {
            "img" => [".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp"],
            "video" => [".mp4", ".mkv", ".avi", ".mov", ".wmv"],
            "audio" => [".mp3", ".wav", ".m4a", ".flac", ".aac"],
            _ => []
        };
    }

    private async Task<string> PickAndUploadFile(IList<string> filter, PickerLocationId location)
    {
        try
        {
            var picker = new FileOpenPicker(XamlRoot.ContentIslandEnvironment.AppWindowId)
            {
                CommitButtonText = "上传",
                SuggestedStartLocation = location
            };
            picker.FileTypeFilter.AddRange(filter);
            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                status.Text = "正在上传文件。请稍作等待";
                var url = await UploadFileAsync(file.Path);
                if (url != "0" && url.Contains("file"))
                {
                    status.Text = "上传成功:" + file.Path;
                    return url;
                }
            }
        }
        catch (Exception ex)
        {
            await App.Logger.WriteAsync("UBBEditor", "文件上传出错", ex.Message);
            status.Text = "上传失败:" + ex.Message;
        }

        return "0";
    }

    private async Task SendReply()
    {
        var url = ApiEndpoints.Topic.SendReply(NavigationInfo.TopicId);
        Dictionary<string, object> reply;
        if (IsTailVisible) Content = Content + "\n" + Tail;
        if (NavigationInfo.EditorMode == EditorMode.ReplyToPost)
            reply = new()
            {
                { "clientType", 1 },
                { "content", Content },
                { "contentType", ContentType },
                { "isAnonymous", IsAnonymous },
                { "notifyAllReplier", NotifyAllReplier },
                { "title", "" },
                { "parentId", NavigationInfo.ParentId }
            };
        else
            reply = new()
            {
                { "clientType", 1 },
                { "content", Content },
                { "contentType", ContentType },
                { "isAnonymous", IsAnonymous },
                { "notifyAllReplier", NotifyAllReplier },
                { "title", "" }
            };
        var replyText = SerializationHelper.TrySerialize(reply);
        var requestBody = new StringContent(replyText, Encoding.UTF8, "application/json");
        var res = await ApiService.Submit<int>(url, requestBody);
        if (!res.IsSuccess)
        {
            //
            status.Text = $"发送失败:{res.Message}";
            await App.Logger.WriteAsync("UBBEditor", "发送回复出错", res.Message);
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
        var post = new Dictionary<string, object>
        {
            { "clientType", 1 },
            { "content", Content },
            { "contentType", ContentType },
            { "isAnonymous", IsAnonymous },
            { "notifyPoster", NotifyPoster },
            { "title", SetTitle.Text },
            { "type", PostTypeValue }
        };
        var text = SerializationHelper.TrySerialize(post);
        var requestBody = new StringContent(text, Encoding.UTF8, "application/json");
        var res = await ApiService.Submit<int>(url, requestBody);
        if (!res.IsSuccess)
        {
            //
            status.Text = $"发送新主题失败:{res.Message}";
            await App.Logger.WriteAsync("UBBEditor", "发送新主题失败", res.Message);
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

    private void PriviewMode_Click(object sender, RoutedEventArgs e)
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
        if (ContentType == 0)
        {
            ContentType = 1;
            MD.Visibility = Visibility.Visible;
            UBB.Visibility = Visibility.Collapsed;
            MdViewer.Visibility = Visibility.Visible;
            UbbViewer.Visibility = Visibility.Collapsed;
            Flower.Play("\uE946", "切换到Markdown");
        }
        else
        {
            ContentType = 0;
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