
using CC98.Kernel;
using CC98.Kernel.ApiScope;
using CC98.Objects;
using CommunityToolkit.Mvvm.ComponentModel;
using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Game : Page
    {
        public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
        public ObservableCollection<Objects.Card> cards = [];
        public CardStat CardDrawStat { get; set; }=new CardStat() { Wealth=0,CardCount=0,DrawCount=0,TotalBonus=0,TotalCost=0};
        public ObservableCollection<GachaInfo> gachaInfo1 = new();
        public ObservableCollection<GachaInfo> gachaInfo2 = new();
        public Game()
        {
            this.InitializeComponent();
            DisplayOdd();
            RefreshStat();
            CardList.ItemsSource = cards;
            
        }
        private async void RefreshStat()
        {
            string profileUrl = ApiEndpoints.User.UserProfile(true, 0);
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
            gachaInfo1.Add(new GachaInfo { Rank = "Mystery", Probability = "0.03%" });
            gachaInfo1.Add(new GachaInfo{Rank = "SSR",Probability = "1.50%"});
            gachaInfo1.Add(new GachaInfo { Rank = "SR", Probability = "15.00%" });
            gachaInfo1.Add(new GachaInfo { Rank = "R", Probability = "29.99%" });
            gachaInfo1.Add(new GachaInfo { Rank = "N", Probability = "53.48%" });
            

            gachaInfo2.Add(new GachaInfo { Rank = "Mystery", Probability = "0.01%" });
            gachaInfo2.Add(new GachaInfo { Rank = "SSR", Probability = "1.50%" });
            gachaInfo2.Add(new GachaInfo { Rank = "SR", Probability = "15.00%" });
            gachaInfo2.Add(new GachaInfo { Rank = "R", Probability = "30.00%" });
            gachaInfo2.Add(new GachaInfo { Rank = "N", Probability = "53.49%" });
            
        }
        private async void StartDraw(int rule)
        {
            cards.Clear();
            string drawUrl = ApiEndpoints.Forum.DraWCard(rule);
            var drawResult = await RequestSender.Fetch<List<Objects.Card>>(drawUrl);
            if (!drawResult.IsSuccess || drawResult.Data == null)
            {
                //
                return;
            }
            var data=drawResult.Data;
            foreach(var card in data)
            {
                card.ImageUri = $"https://card.cc98.org{card.ImageUri.Substring(1, card.ImageUri.Length - 1)}";

            }
            cards.AddRange(data);
            
        }
        
        
        private void FlipSide_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var Flipper = sender as FlipSide;
            if (Flipper != null)
            {
                Flipper.IsFlipped = !Flipper.IsFlipped;
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
            foreach(var c in cards)
            {
                c.IsFlipped = true;
            }
        }

        

        

        private async void Operate_Click(object sender, RoutedEventArgs e)
        {
            var b = sender as Button;
            if (b != null)
            {
                var _tag = b.Tag;
                if (_tag is string tag)
                {
                    switch (tag)
                    {
                        case "single-more":
                            if (SingleProperList.ItemsSource == null)
                            {
                                SingleProperList.ItemsSource = gachaInfo1;
                            }
                            else
                            {
                                SingleProperList.ItemsSource = null;
                            }
                            break;
                        case "multi-more":
                            if (MultiProperList.ItemsSource == null)
                            {
                                MultiProperList.ItemsSource = gachaInfo1;
                            }
                            else
                            {
                                MultiProperList.ItemsSource = null;
                            }
                            break;
                        case "destroy-all":
                            DestroyCardDialog.XamlRoot = this.XamlRoot;
                            var r = await DestroyCardDialog.ShowAsync();
                            if (r == ContentDialogResult.Primary)
                            {
                                string url = "https://card.cc98.org/api/collection/all-rest";
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
           
            
        }

    }
    
    
}
