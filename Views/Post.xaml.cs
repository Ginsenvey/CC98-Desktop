
using CC98.Kernel;
using CC98.Kernel.ApiScope;
using CC98.Kernel.UserExperience;
using CC98.Objects;
using CC98.Services.Extensions;
using ColorCode.Compilation.Languages;
using CommunityToolkit.WinUI.Controls;
using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Storage.Pickers;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

using WinRT.Interop;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class UBBEditor : Page
    {
        public ApplicationDataContainer Set;
        public UBBEditor()
        {
            this.InitializeComponent();
            Set = ApplicationData.Current.LocalSettings;
            LoadEmojiSet("CC98");
        }
        //约定:所有参数必须与json中的实际类型一致
        public string Mode = "0";
        public string Id = "-1";
        public int contentType = 0;//UBB
        public int postType = 0;//普通帖子
        public bool isAnonymous = false;
        public bool NotifyPoster = true;
        public bool notifyAllReplier = false;
        public List<Emoji> emojis = [];
        public List<Emoji> CustomEmojiList = [];
        public string content = "";
        public bool isTailVisible = false;
        public const string tail = "[align=right][size=3][color=gray]——来自「[b][color=purple]CC98 For Windows[/color][/b]」[/color][/size][/align]";
        public EditorNavigationInfo NavigationInfo { get; set; }

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var parameter = e.Parameter as Dictionary<string, string>;
            var args = e.TryGetParameter<EditorNavigationInfo>();
            if (args == null) return;
            NavigationInfo = args;
            ApplyEditorEnv();
        }
        //将编辑器模式应用到UI
        private void ApplyEditorEnv()
        {
            if (ValidationHelper.GetValue(Set, "IsTailVisible") == "1")
            {
                isTailVisible = true;
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
                    Previewer.UbbText = Editor.Text.Replace("\r\n", "  \n").Replace("\r", "  \n");
                    Editor.SelectionStart = Editor.Text.Length;
                    replyselector.IsSelected = true;
                    SetTitle.IsEnabled = false;
                    SetPostType.IsEnabled = false;
                    break;
                case EditorMode.DraftNewTopic:
                    status.Text = $"发表新主题";
                    SetTitle.IsEnabled = true;
                    topicselector.IsSelected = true;
                    break;
                case EditorMode.EditMyPost:
                    status.Text = $"编辑帖子:{NavigationInfo.HintText}";
                    Editor.Text = NavigationInfo.BaseText;
                    SetTitle.Text = NavigationInfo.HintText;
                    Previewer.UbbText = Editor.Text.Replace("\r\n", "  \n").Replace("\r", "  \n");
                    Editor.SelectionStart = Editor.Text.Length;
                    replyselector.IsSelected = true;
                    SetTitle.IsEnabled = false;
                    SetPostType.IsEnabled = false;
                    break;
            }
        }
        private void ClosePanel_Click(object sender, RoutedEventArgs e)
        {
            EditArea.IsPaneOpen = false;
        }
        public string currentLabel = "";//记录实时指令
        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var b = sender as AppBarButton;
            if (b != null)
            {
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
                            string colorwithalpha = Colors.Color.ToString().ToLower();
                            string color = colorwithalpha.Substring(0, 1) + colorwithalpha.Substring(3, 6);
                            InsertTag("color=" + color, "color", "");
                        }
                        break;
                    case "图片":
                        currentLabel = "img";
                        FileHelper.XamlRoot = this.XamlRoot;
                        await FileHelper.ShowAsync();
                        break;
                    case "视频":
                        currentLabel = "video";
                        FileHelper.XamlRoot = this.XamlRoot;
                        await FileHelper.ShowAsync();
                        break;
                    case "音频":
                        currentLabel = "audio";
                        FileHelper.XamlRoot = this.XamlRoot;
                        await FileHelper.ShowAsync();
                        break;
                    case "哔哩":
                        InsertTag("bili", "bili", "");
                        break;
                    case "文档":
                        currentLabel = "upload";
                        FileHelper.XamlRoot = this.XamlRoot;
                        await FileHelper.ShowAsync();
                        break;
                    case "分割线":
                        int selectionStart = Editor.SelectionStart;
                        Editor.Text = Editor.Text.Insert(selectionStart, "[line]");
                        Editor.SelectionStart = selectionStart + 6;
                        Editor.Focus(FocusState.Programmatic);
                        break;
                    case "贴图":
                        await MapPanel.ShowAsync();
                        break;
                    default:
                        break;
                }
            }

        }
        private async Task<string> PickAndUploadFile(IList<string> filter, PickerLocationId location)
        {
            try
            {
                var picker = new FileOpenPicker(this.XamlRoot.ContentIslandEnvironment.AppWindowId);
                picker.CommitButtonText = "上传";
                picker.SuggestedStartLocation = location;
                picker.FileTypeFilter.AddRange(filter);
                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    status.Text = "正在上传文件。请稍作等待";
                    string url = await UploadFileAsync(file.Path);
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
        private void InsertTag(string ltag, string rtag, string input, int offset = 0, bool select = true)
        {
            string openTag = $"[{ltag}]";
            string closeTag = $"[/{rtag}]";
            string fullTag = $"{openTag}{input}{closeTag}";
            int selectionStart = Editor.SelectionStart;
            Editor.Text = Editor.Text.Insert(selectionStart, fullTag);
            Editor.SelectionStart = selectionStart + openTag.Length + input.Length + offset;
            if (select)
            {
                Editor.SelectionLength = input.Length;
            }
            else
            {
                Editor.SelectionLength = 0;
            }
            Editor.Focus(FocusState.Programmatic);

        }
        private void Editor_TextChanged(object sender, TextChangedEventArgs e)
        {
            content = Editor.Text.Replace("\r\n", "\n").Replace("\r", "\n");
            //关闭预览窗格可以避免卡顿，尤其是在内容较长时
            if (!EditArea.IsPaneOpen) return;
            Previewer.UbbText = content;
        }
        //以下方法用于创建Md的代码块,但是UBB编辑器不需要支持这个操作。
        private void InsertCodeBlock()
        {
            int cursorPos = Editor.SelectionStart;
            int selectionLen = Editor.SelectionLength;
            string originalText = Editor.Text;
            string textWithoutSelection = originalText.Remove(cursorPos, selectionLen);
            string codeBlock = "`" + "`";
            string newText = textWithoutSelection.Insert(cursorPos, codeBlock);
            Editor.Text = newText;
            int newCursorPos = cursorPos + 1;
            Editor.SelectionStart = newCursorPos;
            Editor.Focus(FocusState.Programmatic);
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
                    break;
                case EditorMode.DraftNewTopic:
                    break;
            }
        }
        private async Task<string> UploadFileAsync(string filePath)
        {
            string url = ApiEndpoints.Forum.UploadFile();
            using (var formData = new MultipartFormDataContent())
            {
                var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data");
                formData.Add(fileContent, "files", Path.GetFileName(filePath));
                var res = await RequestSender.Submit<List<string>>(url, formData);
                if (!res.IsSuccess || res.Data == null)
                {
                    //
                    status.Text = $"上传失败:{res.Message}";
                    await App.Logger.WriteAsync("UBBEditor", "上传文件失败", res.Message);
                    return "";
                }
                var data = res.Data;
                if (data.Count > 0)
                {
                    return data[0];
                }
                else
                {
                    await App.Logger.WriteAsync("UBBEditor", "上传文件出错", "服务器未返回文件地址");
                    return "";
                }
            }
        }
        private async Task SendReply()
        {
            string url = ApiEndpoints.Topic.SendReply(NavigationInfo.TopicId);
            Dictionary<string, object> reply;
            if (isTailVisible)
            {
                content = content + "\n" + tail;
            }
            if (NavigationInfo.EditorMode == EditorMode.ReplyToPost)
            {
                reply = new Dictionary<string, object>()
                {
                {"clientType",1 },
                {"content",content },
                {"contentType",contentType },
                {"isAnonymous",isAnonymous },
                {"notifyAllReplier",notifyAllReplier },
                {"title","" },
                {"parentId",NavigationInfo.ParentId }
                };
            }
            else
            {
                reply = new Dictionary<string, object>()
            {
                {"clientType",1 },
                {"content",content },
                {"contentType",contentType },
                {"isAnonymous",isAnonymous },
                {"notifyAllReplier",notifyAllReplier },
                {"title","" }
            };
            }
            string replyText = JsonSerialize.Serialize(reply);
            await App.Logger.WriteAsync("Editor", "回复内容", replyText);
            var requestBody = new StringContent(replyText, Encoding.UTF8, "application/json");
            var res = await RequestSender.Submit<int>(url, requestBody);
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
                    TargetFloor = NavigationInfo.Floor
                };
                Frame.Navigate(typeof(Topic), param);
            }
        }
        private List<string> GetSuffixs(string type)
        {
            return type switch
            {
                "img" => new List<string> { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp" },
                "video" => new List<string> { ".mp4", ".mkv", ".avi", ".mov", ".wmv" },
                "audio" => new List<string> { ".mp3", ".wav", ".m4a", ".flac", ".aac" },
                _ => new List<string>()
            };
        }

        private void LoadEmojiSet(string type)
        {
            EmojiContainer.ItemsSource = null;
            string EmojiPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Emoji", type);
            var Files = Directory.GetFiles(EmojiPath, "*", SearchOption.TopDirectoryOnly);
            emojis = new List<Emoji>();
            foreach (var file in Files)
            {
                string filename = Path.GetFileName(file);
                emojis.Add(new Emoji { EmojiName = filename.Split(".")[0].ToLower(), EmojiPath = file });
            }
            EmojiContainer.ItemsSource = emojis;
        }

        private async void FileUploadChoice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FileUploadChoice.SelectedIndex != -1)
            {
                var item = FileUploadChoice.SelectedItem as ListViewItem;
                if (item == null) return;
                var tag = item?.Tag;
                if (tag == null) return;
                int operation = tag.ToInt();
                if (operation == 1)
                {
                    var url = "";
                    var filter = GetSuffixs(currentLabel);
                    switch (currentLabel)
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
                    {
                        InsertTag(currentLabel, currentLabel, url);
                    }
                    else
                    {
                        Flower.Play(FlowStatus.Info, "未上传文件");
                    }
                }
                else if (operation == 2)
                {
                    FileHelper.Hide();
                    InsertTag(currentLabel, currentLabel, "");
                }
                else
                {
                    CustomLink.Visibility = Visibility.Visible;
                    CustomLink.Focus(FocusState.Keyboard);//自动聚焦，减少鼠标操作
                }
            }

        }





        private void EmojiContainer_ItemClick(object sender, ItemClickEventArgs e)
        {
            var i = e.ClickedItem;
            if (i != null)
            {

                var index = EmojiContainer.Items.IndexOf(i);
                if (emojis != null)
                {
                    if (emojis.Count > index)
                    {
                        var tag = emojis[index].EmojiName;
                        if (tag != null)
                        {
                            int selectionStart = Editor.SelectionStart;
                            string EmojiName = "[" + tag + "]";
                            Editor.Text = Editor.Text.Insert(selectionStart, EmojiName);
                            Editor.SelectionStart = selectionStart + EmojiName.Length;
                            Editor.Focus(FocusState.Programmatic);

                        }
                    }
                }
            }

        }



        #region UI事件处理
        private void PriviewMode_Click(object sender, RoutedEventArgs e)
        {
            EditArea.IsPaneOpen = !EditArea.IsPaneOpen;
        }


        private void EmojiType_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            var type = sender as SegmentedItem;
            if (type != null)
            {
                type.IsSelected = true;
                if (type.Tag.ToString() != null)
                {
                    LoadEmojiSet(type.Tag.ToString());
                }


            }
        }
        private void SwitchContentType_Click(object sender, RoutedEventArgs e)
        {
            if (contentType == 0)
            {
                contentType = 1;
                MD.Visibility = Visibility.Visible;
                UBB.Visibility = Visibility.Collapsed;
                Flower.Play("\uE946", "切换到Markdown");
            }
            else
            {
                contentType = 0;
                MD.Visibility = Visibility.Collapsed;
                UBB.Visibility = Visibility.Visible;
                Flower.Play("\uE946", "切换到UBB");
            }
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
            if (r != null)
            {
                var tag = r.Tag;
                if (tag != null)
                {
                    var _tag = tag.ToString();
                    if (_tag != null)
                    {
                        postType = Convert.ToInt16(tag);
                    }
                }

            }
        }


        private void MapContainer_ItemClick(object sender, ItemClickEventArgs e)
        {
            var i = e.ClickedItem;
            if (i != null)
            {

                var index = MapContainer.Items.IndexOf(i);
                if (CustomEmojiList != null)
                {

                    if (CustomEmojiList.Count > index)
                    {
                        var tag = CustomEmojiList[index].EmojiPath;
                        if (tag != null)
                        {
                            InsertTag("img", "img", tag, 6, false);
                        }
                    }
                }
            }
        }

        private void MapPanel_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            var list = CustomEmoji.GetAllEmoji();
            CustomEmojiList = new List<Emoji>();
            int i = 0;
            foreach (var e in list)
            {
                CustomEmojiList.Add(new Emoji { EmojiName = i.ToString(), EmojiPath = e });
                i++;
            }
            MapContainer.ItemsSource = CustomEmojiList;
        }

        private void ConfirmCustomLink_Click(object sender, RoutedEventArgs e)
        {
            InsertTag(currentLabel, currentLabel, CustomLink.Text);
            FileHelper.Hide();
        }

        private void FileHelper_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            CustomLink.Visibility = Visibility.Collapsed;
            FileUploadChoice.SelectedIndex = -1;
        }
        #endregion
    }
    public class Emoji
    {
        public string EmojiName {  get; set; }
        public string EmojiPath { get; set; }
    }
    public class PostTag
    {
        public string Name { get; set; }
        public int Id { get; set; }
    }
}
