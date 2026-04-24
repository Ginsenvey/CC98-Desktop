using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Storage;
using CC98.Kernel;
using CC98.Objects;
using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class Game : Page
{
    public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
    public ObservableCollection<Objects.Card> Cards = [];
    public CardStat CardDrawStat { get; set; }=new() { Wealth=0,CardCount=0,DrawCount=0,TotalBonus=0,TotalCost=0};
    public ObservableCollection<GachaInfo> GachaInfo1 = [];
    public ObservableCollection<GachaInfo> GachaInfo2 = [];
    public Game()
    {
        InitializeComponent();
        DisplayOdd();
        RefreshStat();
        CardList.ItemsSource = Cards;
            
    }
    private async void RefreshStat()
    {
        var profileUrl = ApiEndpoints.User.UserProfile(true, 0);
        var profileResult = await RequestSender.Fetch<UserInfo>(profileUrl);
        if (!profileResult.IsSuccess || profileResult.Data == null)
        {
            return;
        }
        var data = profileResult.Data;
        CardDrawStat.Wealth = data.Wealth;
        var statUrl = ApiEndpoints.Forum.CardStat();
        var statResult = await RequestSender.Fetch<CardStat>(statUrl);
        if (!statResult.IsSuccess || statResult.Data == null)
        {
            //
            return;
        }
        var stat = statResult.Data;
        CardDrawStat.DrawCount = stat.DrawCount;
        CardDrawStat.TotalCost = stat.TotalCost;
        CardDrawStat.TotalBonus = stat.TotalBonus;
        CardDrawStat.CardCount = stat.CardCount;          
    }
        
    private void DisplayOdd()
    {
        GachaInfo1.Add(new() { Rank = "Mystery", Probability = "0.03%" });
        GachaInfo1.Add(new() {Rank = "SSR",Probability = "1.50%"});
        GachaInfo1.Add(new() { Rank = "SR", Probability = "15.00%" });
        GachaInfo1.Add(new() { Rank = "R", Probability = "29.99%" });
        GachaInfo1.Add(new() { Rank = "N", Probability = "53.48%" });
            

        GachaInfo2.Add(new() { Rank = "Mystery", Probability = "0.01%" });
        GachaInfo2.Add(new() { Rank = "SSR", Probability = "1.50%" });
        GachaInfo2.Add(new() { Rank = "SR", Probability = "15.00%" });
        GachaInfo2.Add(new() { Rank = "R", Probability = "30.00%" });
        GachaInfo2.Add(new() { Rank = "N", Probability = "53.49%" });
            
    }
    private async void StartDraw(int rule)
    {
        Cards.Clear();
        var drawUrl = ApiEndpoints.Forum.DrawCard(rule);
        var drawResult = await RequestSender.Submit<List<Objects.Card>>(drawUrl,null);
        if (!drawResult.IsSuccess || drawResult.Data == null)
        {
            //
            Flower.Play(FlowStatus.Fail, drawResult.Message);
            return;
        }
        var data=drawResult.Data;
        foreach(var card in data)
        {
            card.ImageUri = $"https://card.cc98.org{card.ImageUri.Substring(1, card.ImageUri.Length - 1)}";
        }
        Cards.AddRange(data);
            
    }
        
        
    private void FlipSide_Tapped(object sender, TappedRoutedEventArgs e)
    {
        var flipper = sender as FlipSide;
        if (flipper != null)
        {
            flipper.IsFlipped = !flipper.IsFlipped;
        }
    }

    private async void Draw_Click(object sender, RoutedEventArgs e)
    {
        BusyIndicator.Visibility = Visibility.Visible;
        ResultViewer.Visibility = Visibility.Collapsed;
        StartDraw(1);
        await Task.Delay(1000);
        BusyIndicator.Visibility = Visibility.Collapsed;
        ResultViewer.Visibility = Visibility.Visible;
    }

    private async void Drawn_Click(object sender, RoutedEventArgs e)
    {
        BusyIndicator.Visibility = Visibility.Visible;
        ResultViewer.Visibility = Visibility.Collapsed;
        StartDraw(2);
        await Task.Delay(1500);
        BusyIndicator.Visibility = Visibility.Collapsed;
        ResultViewer.Visibility = Visibility.Visible;
    }

    private void Unfold_Click(object sender, RoutedEventArgs e)
    {
        foreach(var c in Cards)
        {
            c.IsFlipped = true;
        }
    }

        

        

    private async void Operate_Click(object sender, RoutedEventArgs e)
    {
        var b = sender as Button;
        if (b == null || b.Tag is not string tag) return;

        switch (tag)
        {
            case "single-more":
                if (SingleProperList.ItemsSource == null)
                {
                    SingleProperList.ItemsSource = GachaInfo1;
                }
                else
                {
                    SingleProperList.ItemsSource = null;
                }
                break;
            case "multi-more":
                if (MultiProperList.ItemsSource == null)
                {
                    MultiProperList.ItemsSource = GachaInfo1;
                }
                else
                {
                    MultiProperList.ItemsSource = null;
                }
                break;
            case "destroy-all":
                DestroyCardDialog.XamlRoot = XamlRoot;
                var r = await DestroyCardDialog.ShowAsync();
                if (r == ContentDialogResult.Primary)
                {
                    var url = "https://card.cc98.org/api/collection/all-rest";
                    var result = await RequestSender.Delete(url);
                    if (!result.IsSuccess)
                    {
                        //
                        Flower.Play("\uEA39", "分解失败");
                        return;
                    }
                    RefreshStat();
                    Flower.Play("\uE930", "分解成功");
      
                }
                break;
            case "ref-stat":
                RefreshStat();
                Flower.Play("\uE930", "正在刷新数据");
                break;
        }


    }

}