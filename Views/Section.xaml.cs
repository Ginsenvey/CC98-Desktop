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
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CC98.Kernel;
using CC98.Kernel.ApiScope;
using System.Text.Json;
using DevWinUI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Section : Page
    {
        public ApplicationDataContainer Set=ApplicationData.Current.LocalSettings;
        public ObservableCollection<SectionInfo> allSections;
        public Section()
        {
            this.InitializeComponent();
            allSections = new ObservableCollection<SectionInfo>()
            {
                
            };
            SectionPresenter.ItemsSource = allSections;
            
            GetAllSection();
            LoadSet();
        }
        private void LoadSet()
        {
            var _Theme = ValidationHelper.GetValue(Set, "ThemePic");
            if (_Theme != "0")
            {
                //ThemePresenter.Source = new BitmapImage(new Uri(_Theme));
            }
        }
        private async Task<bool> FetchSection()
        {
            string url = ApiEndpoints.Board.AllBoards();
            var res = await RequestSender.Fetch<string>(url);
            if (res.IsNotValid)
            {
                Flower.PlayAnimation("\uEA39", "更新首页缓存失败");
                return false;
            }
            var data = res.Data;
            ValidationHelper.JsonWritter(data, "SectionCache.json");
            return true;
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
        private void LoadSection(string sectionJson)
        {
            var data = JsonSerializer.Deserialize<List<SectionInfo>>(sectionJson);
            if (data != null)
            {
                allSections.AddRange(data);
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
                    Flower.PlayAnimation("\uE930", "刷新版面成功");
                }
            }
        }
    }
    public class SectionInfo
    {
        public string SectionName { get; set; } = string.Empty;
        public List<BoardInfo> Boards {  get; set; }= new List<BoardInfo>();
    }
    public class BoardInfo
    {
        public string BoardName { get; set; }=string.Empty;
        public int BoardId { get; set; }
    }
}
