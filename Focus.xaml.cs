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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using CommunityToolkit.Labs.WinUI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App3
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Focus : Page
    {
        public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
        public Focus()
        {
            this.InitializeComponent();
            if (Set.Values.ContainsKey("CustomBoards"))
            {
                if (Set.Values["CustomBoards"].ToString() != "0")
                {
                    memory = JsonConvert.DeserializeObject<Dictionary<string, string>>(Set.Values["CustomBoards"].ToString());
                }
                
                
            }
            else
            {
                //初始化本地缓存
                Set.Values["CustomBoards"] = "0";
            }
            InitializeClient();
            GetFocusBoards();
        }
        private void InitializeClient()
        {
            if (Set.Values.ContainsKey("Access"))
            {
                MainWindow.loginservice.client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Set.Values["Access"] as string);
            }

        }
        private async void GetFocusBoards()
        {
            string ProfileUrl = "https://api.cc98.org/me";
            var ProfileRes = await MainWindow.loginservice.client.GetAsync(ProfileUrl);
            if (ProfileRes.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string ProfileText = await ProfileRes.Content.ReadAsStringAsync();
                var js = JsonConvert.DeserializeObject<Dictionary<string, object>>(ProfileText);
                var boards = JsonConvert.DeserializeObject<JArray>(js["customBoards"].ToString());
                Dictionary<string, string> CustomBoards = new();
                foreach (var b in boards)
                {
                    AddBoards(b.ToString());
                }

                

            }
        }
        public Dictionary<string, string> memory = new();
        private async void AddBoards(string BoardId)
        {
            //先判断本地存储是否有此板块
            if (!memory.ContainsKey(BoardId))
            {
                string BoardUrl = "https://api.cc98.org/board/" + BoardId;
                try
                {
                    var BoardRes = await MainWindow.loginservice.client.GetAsync(BoardUrl);
                    if (BoardRes.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string BoardText = await BoardRes.Content.ReadAsStringAsync();
                        if (!string.IsNullOrEmpty(BoardText))
                        {
                            var js = JsonConvert.DeserializeObject<Dictionary<string, object>>(BoardText);
                            if (js != null)
                            {
                                string name = js["name"].ToString();
                                memory.Add(BoardId, name);
                                Focuses.Items.Add(new TokenItem { Content = name, Tag = BoardId });
                                string boardjsontext = JsonConvert.SerializeObject(memory);
                                Set.Values["CustomBoards"] = boardjsontext;
                            }
                        }
                    }
                }
                catch
                {

                }
            }
            else
            {
                //如果本地存储有此板块，则直接添加
                Focuses.Items.Add(new TokenItem { Content = memory[BoardId], Tag = BoardId });
            }
        }
    }
}
