using CCkernel;
using CCUserModel;
using ColorCode.Compilation.Languages;
using CommunityToolkit.WinUI.Controls;
using CommunityToolkit.WinUI.UI.Controls;
using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using static App3.Topic;
using static System.Net.Mime.MediaTypeNames;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App3
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Post : Page
    {
        public ApplicationDataContainer Set;
        public Post()
        {
            this.InitializeComponent();
            Set= ApplicationData.Current.LocalSettings;
            LoadEmojiSet("CC98");
        }
        //约定:所有参数必须与json中的实际类型一致
        public string Mode = "0";
        public string Id = "-1";
        public int Content_Type = 0;//UBB
        public int Post_Type = 0;//普通帖子
        public bool IsAnonymous=false;
        public bool NotifyPoster=true;
        public bool NotifyReplier = false;
        public List<Emoji> emojis;
        public List<Emoji> CustomEmojiList;
        public string Parent_Id = "";
        
        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var parameter = e.Parameter as Dictionary<string,string>;

            if (parameter != null)
            {
                string mode= parameter["Mode"];
                Mode = mode;
                if (mode == "0")//回复主题
                {
                    
                    status.Text = "回复主题:" + parameter["Pid"];
                    Id= parameter["Pid"];
                    replyselector.IsSelected = true;
                    SetTitle.IsEnabled = false;
                    SetContentType.IsEnabled = false;
                }
                else if(mode == "1")//引用回复
                {
                    status.Text = "回复帖子:" + parameter["QuoteText"];
                    Id = parameter["Pid"];
                    Editor.Text = $"[quote]{parameter["Header"]}{parameter["QuoteText"]}[/quote]";
                    Previewer.Text = UBBConverter.Convert(Editor.Text.Replace("\r\n", "  \n").Replace("\r", "  \n"), false);
                    Editor.SelectionStart = Editor.Text.Length;
                    Parent_Id = parameter["ParentId"];
                    replyselector.IsSelected = true;
                    SetTitle.IsEnabled = false;
                    SetContentType.IsEnabled = false;
                }
                else if(mode == "2")//发帖
                {
                    status.Text = "发表主题:" + parameter["BoardId"];
                    Id = parameter["BoardId"];
                    SetTitle.IsEnabled = true;
                    topicselector.IsSelected= true;
                }
            }
            else
            {

            }
        }
        private void ClosePanel_Click(object sender, RoutedEventArgs e)
        {
            EditArea.IsPaneOpen = false;
        }
        public string CurrentLabel = "";//记录实时指令
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
                        InsertTag("b","b" ,"");
                        break;
                    case "斜体":
                        InsertTag("i","i", "");
                        break;
                    case "删除线":
                        InsertTag("del","del", "");
                        break;
                    case "下划线":
                        InsertTag("u","u" ,"");
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
                        var r=await ColorPanel.ShowAsync();
                        if(r == ContentDialogResult.Primary)
                        {
                            string colorwithalpha = Colors.Color.ToString().ToLower();
                            string color = colorwithalpha.Substring(0, 1) + colorwithalpha.Substring(3, 6);
                            InsertTag("color=" + color, "color", "");
                        }
                        break;
                    case "图片":
                        CurrentLabel = "img";
                        FileHelper.XamlRoot = this.XamlRoot;
                        await FileHelper.ShowAsync();
                        break;
                    case "视频":
                        CurrentLabel = "video";
                        FileHelper.XamlRoot = this.XamlRoot;
                        await FileHelper.ShowAsync();
                        break;
                    case "音频":
                        CurrentLabel = "audio";
                        FileHelper.XamlRoot = this.XamlRoot;
                        await FileHelper.ShowAsync();      
                        break;
                    case "哔哩":
                        InsertTag("bili", "bili", "");
                        break;
                    case "文档":
                        CurrentLabel = "upload";
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
        private async Task<string> FileSender(string type,IList<string> filter,string title,Windows.Storage.Pickers.PickerLocationId location)
        {
            var picker = new SavePicker(WindowNative.GetWindowHandle((App.Current as App).m_window))
            {
               
            };
            picker.Title = title;
            picker.FileTypeChoices.Add(type,filter);
            picker.CommitButtonText = "上传";
            picker.SuggestedStartLocation= location;
            var file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                status.Text = "正在上传文件。请稍作等待";
                string url = await UploadFileAsync("https://api.cc98.org/file", file.Path);
                if (url != "0" && url.Contains("file"))
                {
                    status.Text = "上传成功:" + file.Path;
                    return url;
                }
            }
            return "0";
        }
        private void InsertTag(string ltag,string rtag, string input,int offset=0,bool select=true)
        {
            string openTag = $"[{ltag}]";
            string closeTag = $"[/{rtag}]";
            string fullTag = $"{openTag}{input}{closeTag}";         
            int selectionStart = Editor.SelectionStart;
            Editor.Text = Editor.Text.Insert(selectionStart, fullTag);
            Editor.SelectionStart = selectionStart + openTag.Length+input.Length+offset;
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
            Previewer.Text = UBBConverter.Convert(Editor.Text.Replace("\r\n", "  \n").Replace("\r","  \n"),false);
            
        }
        //以下方法用于创建Md的代码块,但是UBB编辑器不需要支持这个操作。
        private void InsertCodeBlock()
        {
            int cursorPos = Editor.SelectionStart;
            int selectionLen = Editor.SelectionLength;
            string originalText = Editor.Text;
            string textWithoutSelection = originalText.Remove(cursorPos, selectionLen);
            string codeBlock = "`" +  "`";
            string newText = textWithoutSelection.Insert(cursorPos, codeBlock);
            Editor.Text = newText;  
            int newCursorPos = cursorPos + 1;
            Editor.SelectionStart = newCursorPos;
            Editor.Focus(FocusState.Programmatic);
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var r=await SendDialog.ShowAsync();
            if (r == ContentDialogResult.Primary)
            {
                if (Mode == "0"||Mode=="1")
                {
                    string tail = "[align=right][size=3][color=gray]——来自「[b][color=purple]CC98 For Windows[/color][/b]」[/color][/size][/align]";
                    string maintext = Editor.Text.Replace("\r\n", "\n").Replace("\r", "\n");
                    string target=ValidationHelper.IsTokenExist(Set,"IsTailVisible")=="1"?maintext+"\n"+tail:maintext;
                    string MyReplyId=await RequestSender.SendReplyToTopic(Id,target, IsAnonymous,NotifyReplier ,Content_Type,Mode=="1",Parent_Id);
                    //直接构造json字符串和使用jsonconvert序列化的换行符变化方式不同。这里的替换方式适用于序列化。
                    if(MyReplyId.StartsWith("101:"))
                    {
                        status.Text = "发送失败:" + MyReplyId.Split(":")[1];
                    }
                    else if(MyReplyId.StartsWith("400:"))
                    {
                        status.Text = "发生错误。请向开发者报告此问题，日志已记录";
                        ValidationHelper.Log("向主题发送回复出错", MyReplyId.Split(":")[1]);
                    }
                    else
                    {
                        if (Frame.CanGoBack)
                        {
                            Frame.GoBack();
                        }
                        else
                        {
                            Frame.Navigate(typeof(Topic), Id);
                        }
                        
                    }
                }
                else if (Mode == "2")
                {
                    string MyPostId = await RequestSender.SendPost(Id, Editor.Text.Replace("\r\n", "\n").Replace("\r", "\n"), SetTitle.Text, Content_Type, NotifyPoster, Post_Type, IsAnonymous);
                    if(MyPostId.StartsWith("404:"))
                    {
                        status.Text = MyPostId;
                    }
                    else
                    {
                        if (MyPostId.All(char.IsDigit))
                        {
                            Frame.Navigate(typeof (Topic), MyPostId);
                        }
                    }
                }

            }
        }
        private async Task<string> UploadFileAsync(string Url, string filePath)
        {
            
            using (var formData = new MultipartFormDataContent())
            {
                var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data"); 
                formData.Add(fileContent, "files", Path.GetFileName(filePath));
                HttpResponseMessage response = await CCloginservice.vpn.PostAsync(Url, formData);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string res = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(res))
                    {
                        var array = JsonConvert.DeserializeObject<JArray>(res);
                        if (array.Count == 1)
                        {
                            return array[0].ToString();
                        }
                        else
                        {
                            return "0";
                        }
                    }
                    else
                    {
                        return "0";
                    }
                }
                else
                {
                    return "0";
                }

            }
        }
         

        private void PriviewMode_Click(object sender, RoutedEventArgs e)
        {
            EditArea.IsPaneOpen=!EditArea.IsPaneOpen;
        }
        

        private void EmojiType_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            var type = sender as SegmentedItem;
            if (type != null)
            {
                type.IsSelected = true;
                if (type.Tag.ToString()!=null)
                {
                    LoadEmojiSet(type.Tag.ToString());
                }
                

            }
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
                if (item != null)
                {
                    string tag = item.Tag.ToString();
                    if (tag == "1")
                    {
                        if (CurrentLabel == "img")
                        {
                            var imagefilter = new List<string> { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif", "*.webp" };
                            string imageurl = await FileSender("图像", imagefilter, "选择一张图片", Windows.Storage.Pickers.PickerLocationId.PicturesLibrary);
                            if (imageurl != "0")
                            {
                                InsertTag("img", "img", imageurl);
                                FileHelper.Hide();
                            }
                            else
                            {
                                FileHelper.Hide();
                                Flower.PlayAnimation("\uEA39", "未上传文件");
                            }

                        }
                        else if (CurrentLabel == "video")
                        {
                            var videofilter = new List<string> { "*.mp4", "*.mkv", "*.avi", "*.mov", "*.wmv" };
                            string videourl = await FileSender("视频", videofilter, "选择视频文件", Windows.Storage.Pickers.PickerLocationId.VideosLibrary);
                            if (videourl != "0")
                            {
                                InsertTag("video", "video", videourl);
                                FileHelper.Hide();
                            }
                            else
                            {
                                FileHelper.Hide();
                                Flower.PlayAnimation("\uEA39", "未上传文件");
                            }
                        }
                        else if (CurrentLabel == "audio")
                        {
                            var audiofilter = new List<string> { "*.mp3", "*.wav", "*.m4a", "*.flac", "*.aac" };
                            string audiourl = await FileSender("音频", audiofilter, "选择y音频文件", Windows.Storage.Pickers.PickerLocationId.MusicLibrary);
                            if (audiourl != "0")
                            {
                                InsertTag("audio", "audio", audiourl);
                                FileHelper.Hide();
                            }
                            else
                            {
                                FileHelper.Hide();
                                Flower.PlayAnimation("\uEA39", "未上传文件");
                            }
                        }
                        else if (CurrentLabel == "upload")
                        {
                            var docfilter = new List<string> { "*" };
                            string docurl = await FileSender("任意文件", docfilter, "选择文件", Windows.Storage.Pickers.PickerLocationId.Desktop);
                            if (docurl != "0")
                            {
                                InsertTag("upload", "upload", docurl);
                                FileHelper.Hide();
                            }
                            else
                            {
                                FileHelper.Hide();
                                Flower.PlayAnimation("\uEA39", "未上传文件");
                            }
                        }

                    }
                    else if (tag == "2")
                    {
                        FileHelper.Hide();
                        InsertTag(CurrentLabel, CurrentLabel, "");
                    }
                    else
                    {
                        CustomLink.Visibility = Visibility.Visible;
                        CustomLink.Focus(FocusState.Keyboard);//自动聚焦，减少鼠标操作
                    }
                }
            }
            
        }

        private void ConfirmCustomLink_Click(object sender, RoutedEventArgs e)
        {
            InsertTag(CurrentLabel, CurrentLabel, CustomLink.Text);
            FileHelper.Hide();
        }

        private void FileHelper_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            CustomLink.Visibility=Visibility.Collapsed;
            FileUploadChoice.SelectedIndex = -1;
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

        private void SwitchContentType_Click(object sender, RoutedEventArgs e)
        {
            if (Content_Type == 0)
            {
                Content_Type = 1;
                MD.Visibility = Visibility.Visible;
                UBB.Visibility = Visibility.Collapsed;
                Flower.PlayAnimation("\uE946", "切换到Markdown");
            }
            else
            {
                Content_Type = 0;
                MD.Visibility = Visibility.Collapsed;
                UBB.Visibility = Visibility.Visible;
                Flower.PlayAnimation("\uE946", "切换到UBB");
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
                    var _tag=tag.ToString();
                    if (_tag != null)
                    {
                        Post_Type = Convert.ToInt16(tag);
                    }
                }
                
            }
        }
        private async void Drawer_ImageResolving(object sender, ImageResolvingEventArgs e)
        {
            var defr = e.GetDeferral();
            var Source = e.Url;
            if (Source == null) return;

            try
            {
                switch (Source)
                {
                    case string url when ImageResolver.IsWebUrl(url):
                        e.Image = await ImageResolver.LoadWebImage(url);
                        break;

                    case string path when ImageResolver.IsLocalPath(path):
                        e.Image = await ImageResolver.LoadLocalImage(path);
                        break;
                }
            }
            catch
            {
                e.Image = null;
            }
            e.Handled = true;
            defr.Complete();

        }

        private void MapContainer_ItemClick(object sender, ItemClickEventArgs e)
        {
            var i = e.ClickedItem;
            if (i != null)
            {

                var index =MapContainer.Items.IndexOf(i);
                if (CustomEmojiList != null)
                {

                    if (CustomEmojiList.Count > index)
                    {
                        var tag = CustomEmojiList[index].EmojiPath;
                        if (tag != null)
                        {
                            InsertTag("img", "img", tag,6,false);
                        }
                    }
                }
            }
        }

        private void MapPanel_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            var list=CustomEmoji.GetAllEmoji();
            CustomEmojiList = new List<Emoji>();
            int i = 0;
            foreach (var e in list)
            {
                CustomEmojiList.Add(new Emoji { EmojiName = i.ToString(), EmojiPath = e });
                i++;
            }
            MapContainer.ItemsSource = CustomEmojiList;
        }
    }
    public class Emoji
    {
        public string EmojiName {  get; set; }
        public string EmojiPath { get; set; }
    }
}
