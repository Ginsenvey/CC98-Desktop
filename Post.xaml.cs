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
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App3
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Post : Page
    {
        public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
        public Post()
        {
            this.InitializeComponent();
            
        }
        
        public string Mode = "0";
        public string Id = "-1";
        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // 获取传递的参数
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

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
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
                    default:
                        break;
                }
            }
            
        }
        private void InsertTag(string ltag,string rtag, string input)
        {
            string openTag = $"  \n[{ltag}]";
            string closeTag = $"[/{rtag}]";
            string fullTag = $"{openTag}{input}{closeTag}";

            // 获取当前光标位置
            int selectionStart = Editor.SelectionStart;

            // 插入标签并移动光标到占位符位置
            Editor.Text = Editor.Text.Insert(selectionStart, fullTag);
            Editor.SelectionStart = selectionStart + openTag.Length;
            Editor.SelectionLength = input.Length;

            // 聚焦到输入框
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

            // 删除选中文本
            string originalText = Editor.Text;
            string textWithoutSelection = originalText.Remove(cursorPos, selectionLen);

            // 插入代码块符号
            string codeBlock = "`" +  "`";
            string newText = textWithoutSelection.Insert(cursorPos, codeBlock);
            Editor.Text = newText;

            // 计算新光标位置
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
                        status.Text = "发送失败，检查网络或者反馈此问题";
                    }
                    else if(MyReplyId == "400")
                    {
                        status.Text = "未登录或者过期。请重启应用";
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
                //请求必须是该编码格式，而不能发送表单.
                //值的格式不是string，比如，false不应为"false"，0不应为"0".
                //contentType即UBB模式/MD格式。不同的格式web渲染实现不同.
                string url = "https://api.cc98.org/topic/" + replyid + "/post";
                var r = await client.PostAsync(url, requestbody);
                if (r.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return await r.Content.ReadAsStringAsync();
                }
                else
                {
                    return "101";//失败
                }
            }
            else
            {
                return "400";//未鉴权
            }


        }
    }
}
