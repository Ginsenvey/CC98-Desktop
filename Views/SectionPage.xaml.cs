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
    
    private static BoardSectionManager Manager => BoardSectionManager.Instance;

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
        var data = await Manager.GetSectionDataAsync();
        if (data != null) AllSections.AddRange(data);
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