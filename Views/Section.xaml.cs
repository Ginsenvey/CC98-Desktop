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
using CC98.Services.Extensions;
using CC98.Objects;

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
        public ObservableCollection<SectionInfo> allSections=[];
        public BoardSectionManager manager = BoardSectionManager.Instance;
        public Section()
        {
            this.InitializeComponent();
            InitializeBoardSectionsAsync();
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
        private async void InitializeBoardSectionsAsync()
        {
            // 检查缓存是否存在
            bool hasCache =  manager.HasValidCacheAsync();

            if (!hasCache)
            {
                // 没有缓存，立即刷新
                await manager.RefreshFromApiAsync(ApiEndpoints.Forum.AllBoards());
            }
            await LoadSection();
        }

        private void BoardButton_Click(object sender, RoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            var tag = h?.Tag;//当前绑定状态下，h没有DataContext.只能使用tag.
            if (tag == null) return;
            Frame.Navigate(typeof(Board),tag.ToInt()); 
        }
        private async Task LoadSection()
        {
            var data = await manager.LoadFromCacheAsync();
            if (data != null)
            {
                allSections.AddRange(data);
            }
        }

        private async void RefreshSection_Click(object sender, RoutedEventArgs e)
        {
            bool success = await manager.RefreshFromApiAsync(ApiEndpoints.Forum.AllBoards());

            if (success)
            {
                // 刷新成功后重新加载数据
                await LoadSection();
            }
        }
    }
}
