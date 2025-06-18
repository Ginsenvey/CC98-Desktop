using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using static App3.Topic;
using CCkernel;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Storage;
using System.Text;
using CommunityToolkit.WinUI.Controls;
using DevWinUI;
using WinRT.Interop;
using static System.Net.Mime.MediaTypeNames;
using ColorCode.Compilation.Languages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        }
        
        public string Mode = "0";
        public string Id = "-1";
        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var parameter = e.Parameter as Dictionary<string,string>;

            if (parameter != null)
            {
                string mode= parameter["Mode"];
                if (mode == "0")
                {
                    
                    status.Text = "回复主题:" + parameter["Pid"];
                    Id= parameter["Pid"];
                    topicselector.IsSelected = true;
                    SetTitle.IsEnabled = false;
                    SetContentType.IsEnabled = false;
                }
                else if(mode == "1")
                {

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
                        InsertCodeBlock();
                        break;
                    case "链接":
                        InsertTag("url", "url", "");
                        break;
                    case "调色盘":
                        var r=await ColorPanel.ShowAsync();
                        if(r == ContentDialogResult.Primary)
                        {
                            string colorwithalpha = Colors.Color.ToString().ToLower();
                            string color = colorwithalpha.Substring(0, 1) + colorwithalpha.Substring(3, 6);
                            InsertTag("color=" + color, "color", "");
                        }
                        break;
                    case "图片":
                        var picker = new FilePicker(WindowNative.GetWindowHandle((App.Current as App).m_window));
                        picker.FileTypeChoices = new Dictionary<string, IList<string>>
{
    { "Images", new List<string> { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif" ,"*.webp"} },
};                      picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
                        picker.CommitButtonText = "上传";
                        picker.Title = "选择一张图片";
                        //98的文件上传方法和这个完全一致，可复用。
                        var file = await picker.PickSingleFileAsync();
                        if (file != null)
                        {
                            status.Text = "正在上传文件。请稍作等待";
                            string url = await UploadImageAsync("https://api.cc98.org/file", file.Path);
                            if (url != "0" &&url.Contains("file"))
                            {
                                status.Text = "上传成功:" + file.Path;
                                InsertTag("img", "img", url);
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            
        }
        private void InsertTag(string ltag,string rtag, string input)
        {
            string openTag = $"[{ltag}]";
            string closeTag = $"[/{rtag}]";
            string fullTag = $"{openTag}{input}{closeTag}";

            
            int selectionStart = Editor.SelectionStart;

           
            Editor.Text = Editor.Text.Insert(selectionStart, fullTag);
            Editor.SelectionStart = selectionStart + openTag.Length;
            Editor.SelectionLength = input.Length;

            
            Editor.Focus(FocusState.Programmatic);
        }
        private void Editor_TextChanged(object sender, TextChangedEventArgs e)
        {
            Previewer.Text = UBBToMarkdownConverter.Convert(Editor.Text.Replace("\r\n", "  \n").Replace("\r","  \n"),false);
        }
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
                if (Mode == "0")
                {
                    string MyReplyId=await SendReply(Id, Editor.Text.Replace("\r\n", "\\n").Replace("\r", "\\n"), "false", "false", "");
                    
                    if(MyReplyId == "101")
                    {
                        status.Text = "网络问题：发送失败";
                    }
                    else if(MyReplyId == "400")
                    {
                        status.Text = "出错了。向开发者报告此问题。";
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
            }
        }
        private async Task<string> UploadImageAsync(string Url, string filePath)
        {
            using (var client = new HttpClient())
            using (var formData = new MultipartFormDataContent())
            {
                // 读取本地图片文件
                var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));

                // 设置请求的媒体类型为图片的类型
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data"); // 这里假设是JPEG格式

                // 给表单项命名
                formData.Add(fileContent, "files", Path.GetFileName(filePath));
                if (Set.Values.ContainsKey("Access"))
                {
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Set.Values["Access"] as string);
                    HttpResponseMessage response = await client.PostAsync(Url, formData);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string ImageUrlRes = await response.Content.ReadAsStringAsync();
                        if (!string.IsNullOrEmpty(ImageUrlRes))
                        {
                            var array = JsonConvert.DeserializeObject<JArray>(ImageUrlRes);
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
                else
                {
                    return "0";
                }
                
            }
        }
        private void PostType_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {

        }

        private void PriviewMode_Click(object sender, RoutedEventArgs e)
        {
            EditArea.IsPaneOpen=!EditArea.IsPaneOpen;
        }
        private async Task<string> SendReply(string replyid, string content, string displaymode, string replymode, string title)
        {
            string access = Set.Values["Access"] as string;
            if (!string.IsNullOrEmpty(access))
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);
                client.DefaultRequestHeaders.Referrer=new Uri("https://www.cc98.org/");
                
                
                var requestbody = new StringContent("{\"content\":\""+content+"\",\"contentType\":0,\"title\":\"\",\"isAnonymous\":"+displaymode+",\"notifyAllReplier\":"+replymode+",\"clientType\":1}", Encoding.UTF8, "application/json");
                //��������Ǹñ����ʽ�������ܷ��ͱ���.
                //ֵ�ĸ�ʽ����string�����磬false��ӦΪ"false"��0��ӦΪ"0".
                //contentType��UBBģʽ/MD��ʽ����ͬ�ĸ�ʽweb��Ⱦʵ�ֲ�ͬ.
                string url = "https://api.cc98.org/topic/" + replyid + "/post";
                var r = await client.PostAsync(url, requestbody);
                if (r.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return await r.Content.ReadAsStringAsync();
                }
                else
                {
                    return "101";
                }
            }
            else
            {
                return "400";
            }


        }

        private void EmojiType_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            var type = sender as SegmentedItem;
            if (type != null)
            {
                type.IsSelected = true;
            }
        }

        
    }
}
