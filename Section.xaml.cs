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
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using System.Runtime.CompilerServices;
using CCkernel;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App3
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Section : Page
    {
        public ApplicationDataContainer Set=ApplicationData.Current.LocalSettings;
        public ObservableCollection<AllSection> allSections;
        public Section()
        {
            this.InitializeComponent();
            allSections = new ObservableCollection<AllSection>()
            {
                
            };
            SectionPresenter.ItemsSource = allSections;
            
            GetAllSection();
        }

        private async Task<bool> FetchSection()
        {
            string SectionText = await RequestSender.SimpleRequest("https://api.cc98.org/Board/all");
            if (!SectionText.StartsWith("404"))
            {
                ValidationHelper.JsonWritter(SectionText, "SectionCache.json");
                return true;
            }
            else
            {
                return false;
                //Flower.PlayAnimation("\uEA39", "更新首页缓存失败");
            }
        }
        private async void GetAllSection()
        {
            StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;
            string path = cacheFolder.Path + "/" + "SectionCache.json";
            string SectionText = ValidationHelper.JsonReader(path);
            if (!SectionText.StartsWith("10"))
            {
                LoadSection(SectionText);
            }
            else
            {
                if (await FetchSection())
                {
                    LoadSection(SectionText);
                }
            }
            
        }

        private void BoardButton_Click(object sender, RoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            var tag = h.Tag as string;//当前绑定状态下，h没有DataContext.只能使用tag.
            if(tag != null)
            {
                Frame.Navigate(typeof(Board),tag); 
            }
        }
        private void LoadSection(string SectionText)
        {
            var SectionArray = Deserializer.ToArray(SectionText);
            if (SectionArray != null)
            {
                if (SectionArray.Count > 0)
                {
                    foreach (var section in SectionArray)
                    {
                        List<BoardInfo> boardinfo = new();
                        var info = JsonConvert.DeserializeObject<Dictionary<string, object>>(section.ToString());
                        string name = info["name"].ToString();
                        string mastertext = info["masters"].ToString();
                        var boards = JsonConvert.DeserializeObject<JArray>(info["boards"].ToString());
                        foreach (var board in boards)
                        {
                            var js = JsonConvert.DeserializeObject<Dictionary<string, object>>(board.ToString());
                            boardinfo.Add(new BoardInfo { BoardName = js["name"].ToString(), BoardId = js["id"].ToString() });
                        }
                        allSections.Add(new AllSection { SectionName = name, Boards = boardinfo });
                    }
                }
            }
        }

        private async void RefreshSection_Click(object sender, RoutedEventArgs e)
        {
            if (await FetchSection())
            {
                StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;
                string path = cacheFolder.Path + "/" + "SectionCache.json";
                string SectionText = ValidationHelper.JsonReader(path);
                if (!SectionText.StartsWith("10"))
                {
                    LoadSection(SectionText);
                }
            }
        }
    }
    public class AllSection
    {
        public string SectionName
        {
            get; set;
        }
        public List<BoardInfo> Boards
        {
            get; set;
        }
    }
    public class BoardInfo
    {
        public string BoardName { get; set; }
        public string BoardId { get; set; }
    }
}
