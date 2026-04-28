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
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class Section : Page
{
    public ApplicationDataContainer Set=ApplicationData.Current.LocalSettings;
    public ObservableCollection<SectionInfo> AllSections=[];
    public BoardSectionManager Manager = BoardSectionManager.Instance;
    public Section()
    {
        InitializeComponent();
        InitializeBoardSectionsAsync();
        LoadSet();
    }
    private void LoadSet()
    {
        var theme = ValidationHelper.GetValue(Set, "ThemePic");
        if (theme != "0")
        {
            //ThemePresenter.Source = new BitmapImage(new Uri(_Theme));
        }
    }
    private async void InitializeBoardSectionsAsync()
    {
        // 检查缓存是否存在
        var hasCache =  Manager.HasValidCacheAsync();

        if (!hasCache)
        {
            // 没有缓存，立即刷新
            await Manager.RefreshFromApiAsync(ApiEndpoints.Forum.AllBoards());
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
        var data = await Manager.LoadFromCacheAsync();
        if (data != null)
        {
            AllSections.AddRange(data);
        }
    }

    private async void RefreshSection_Click(object sender, RoutedEventArgs e)
    {
        var success = await Manager.RefreshFromApiAsync(ApiEndpoints.Forum.AllBoards());

        if (success)
        {
            // 刷新成功后重新加载数据
            await LoadSection();
        }
    }
}