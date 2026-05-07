using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Storage;
using CC98.Kernel;
using CC98.Objects;
using CC98.Services;
using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CC98.Services.Extensions;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98.Views;

/// <summary>
///     分区页面。
/// </summary>
public sealed partial class SectionPage : Page
{
    public ObservableCollection<SectionInfo> AllSections { get; } = [];
    
    private BoardSectionManager Manager => BoardSectionManager.Instance;

    private static ApplicationDataContainer DataContainer => ApplicationData.Current.LocalSettings;

    public SectionPage()
    {
        InitializeComponent();
        LoadSet();
    }

    private void LoadSet()
    {
        var theme = ValidationHelper.GetValue(DataContainer, "ThemePic");
        if (theme != "0")
        {
            //ThemePresenter.Source = new BitmapImage(new Uri(_Theme));
        }
    }
    
    private async Task LoadSection()
    {
        var data = await Manager.LoadFromCacheAsync();
        if (data != null) AllSections.AddRange(data);
    }

    private async void SectionPage_OnLoaded(object sender, RoutedEventArgs e)
    {
        // 检查缓存是否存在
        var hasCache = Manager.HasValidCache;

        if (!hasCache)
            // 没有缓存，立即刷新
            await Manager.RefreshFromApiAsync(ApiEndpoints.Forum.AllBoards);
        await LoadSection();
    }
}