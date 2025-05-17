using CCkernel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using static App3.Index;
using static App3.Topic;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App3
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Discover : Page
    {
        public ApplicationDataContainer Set=ApplicationData.Current.LocalSettings;
        public ObservableCollection<Tile> tiles;
        public ObservableCollection<RandomTile> randomtiles;//必须是可观测集合，否则UI不显示。
        public Discover()
        {
            this.InitializeComponent();
            tiles = new ObservableCollection<Tile>() { };
            LoadSettings();
            randomtiles = new ObservableCollection<RandomTile>();
            Set = ApplicationData.Current.LocalSettings;
            DiscoverList.ItemsSource = tiles;
            RandomTiles.ItemsSource = randomtiles;
            GetNewTopic();
            GetRandomTile();
        }
        private void LoadSettings()
        {
            if (Set.Values.ContainsKey("ThemePic"))
            {
                if (Set.Values["ThemePic"] as string != "0")
                {
                    string pic = (string)Set.Values["ThemePic"];
                    var bitmap = new BitmapImage(new Uri(pic));
                    ThemePresnter.ImageSource = bitmap;
                }
            }
            else
            {
                Set.Values["ThemePic"] = "0";
            }
        }
        private async void GetNewTopic()
        {
            string NewTopicUrl = "https://api.cc98.org/topic/new?from=0&size=20";
            try
            {
                string access = Set.Values["Access"] as string;
                if (!string.IsNullOrEmpty(access))
                {
                    MainWindow.loginservice.client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);
                    var NewRes = await MainWindow.loginservice.client.GetAsync(NewTopicUrl);
                    if (NewRes.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string NewText = await NewRes.Content.ReadAsStringAsync();
                        var js = JsonConvert.DeserializeObject<JArray>(NewText);
                        if (js != null)
                        {
                            if (js.Count > 0)
                            {
                                
                                foreach (var news in js)
                                {
                                    var tile = JsonConvert.DeserializeObject<Dictionary<string, object>>(news.ToString());
                                    string rid = "0";
                                    if (tile["userId"] != null)
                                    {
                                        rid = tile["userId"].ToString();
                                    }
                                    string pid = tile["id"].ToString();
                                    string hit = tile["hitCount"].ToString();
                                    string title = tile["title"].ToString();
                                    string time = tile["time"].ToString();
                                    string reply = tile["replyCount"].ToString();
                                    string author = "匿名";
                                    if (tile["userName"] != null)
                                    {
                                        author = tile["userName"].ToString();
                                    }
                                    
                                    tiles.Add(new Tile { rid = rid, uid = pid, title = title, time = time, hit = hit, reply = reply,author="@ "+author });
                                }
                            }
                        }
                    }
                    else
                    {
                        
                    }
                }
                
            }
            catch(Exception ex)
            {
                de.Text= ex.Message;
            }
        }

        private void NewTileArea_Click(object sender, RoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            var t = h?.DataContext as Tile;
            if (t != null)
            {
                string param = t.uid;
                if (param != null)
                {
                    Frame.Navigate(typeof(Topic), param);
                }
                
            }
        }
        private async void GetRandomTile()
        {
            string NewTopicUrl = "https://api.cc98.org/topic/random-recent?size=10";
            try
            {
                string access = Set.Values["Access"] as string;
                if (!string.IsNullOrEmpty(access))
                {
                    MainWindow.loginservice.client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);
                    var RandomRes = await MainWindow.loginservice.client.GetAsync(NewTopicUrl);
                    if (RandomRes.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string RandomText = await RandomRes.Content.ReadAsStringAsync();
                        var js = JsonConvert.DeserializeObject<JArray>(RandomText);
                        if (js != null)
                        {
                            if (js.Count > 0)
                            {
                                randomtiles.Clear();
                                foreach (var news in js)
                                {
                                    var tile = JsonConvert.DeserializeObject<Dictionary<string, object>>(news.ToString());
                                    string rid = "0";
                                    if (tile["userId"] != null)
                                    {
                                        rid = tile["userId"].ToString();
                                    }
                                    string pid = tile["id"].ToString();
                                    string hit = tile["hitCount"].ToString();
                                    string title = tile["title"].ToString();
                                    string time = tile["time"].ToString();
                                    string reply = tile["replyCount"].ToString();
                                    randomtiles.Add(new RandomTile { title = title, pid = pid, time = time, hit = hit, reply = reply });
                                }
                            }
                        }
                    }
                    else
                    {

                    }
                }

            }
            catch (Exception ex)
            {
                de.Text = ex.Message;
            }
        }

        private void RefRandomTiles_Click(object sender, RoutedEventArgs e)
        {
            GetRandomTile();
        }

        private void RandomTiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RandomTiles.SelectedItem!=null)
            {
                int selected = RandomTiles.SelectedIndex;
                RandomTile selection = randomtiles[selected]; 
                if(selection!= null)
                {
                    if (selection.pid != null)
                    {
                        Frame.Navigate(typeof(Topic), selection.pid);
                    }
                }
                else
                {
                    ShowTips("无数据", "");
                }
            }
            else
            {
                ShowTips("未选中","");
            }
        }
        private void ShowTips(string title,string content)
        {
            msg.Title = title;
            msg.Content = content;
            msg.IsOpen = true;
        }
    }
    public class RandomTile
    {
        public string title { get; set; }
        public string reply { get; set; }
        public string hit { get; set; }
        public string pid { get; set; }
        public string time { get; set; }
    }
}
