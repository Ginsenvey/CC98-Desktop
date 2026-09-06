using System;
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
    
    private static BoardCacheManager Manager => BoardCacheManager.Instance;

    public SectionPage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await LoadSection();
    }
    private async Task LoadSection()
    {
        try
        {
            var data = await Manager.GetSectionDataAsync();
            if (data != null) AllSections.AddRange(data);
        }
        catch (Exception ex)
        {
            // 无缓存且接口失败时 BoardSectionManager 会抛出异常,在此兜底避免崩溃
            System.Diagnostics.Debug.WriteLine($"加载分区失败: {ex.Message}");
        }
    }

    private void BoardButton_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as HyperlinkButton;
        if(button?.Tag is int boardId)
        {
            Frame.Navigate(typeof(BoardPage), boardId);
        }
        
    }
}