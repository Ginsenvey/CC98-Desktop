using CCkernel;
using CommunityToolkit.Mvvm.ComponentModel;
using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App3
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Game : Page
    {
        public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
        public ObservableCollection<Card> cards = new ObservableCollection<Card>();
        public CardDrawStat CardDrawStat { get; set; }=new CardDrawStat() { wealth=0,cardCount=0,drawCount=0,totalBonus=0,totalCost=0};
        public ObservableCollection<Odd> odds1 = new();
        public ObservableCollection<Odd> odds2 = new();
        public Game()
        {
            this.InitializeComponent();
            DisplayOdd();
            RefreshStat();
            CardList.ItemsSource = cards;
            
        }
        private async void RefreshStat()
        {
            string ProfileUrl = "https://api.cc98.org/me";
            string ProfileText = await RequestSender.SimpleRequest(ProfileUrl);
            if (!ProfileText.StartsWith("404:"))
            {
                var js = Deserializer.ToDictionary(ProfileText);
                if (js != null)
                {
                    int wealth = Convert.ToInt32(ValidationHelper.GetKey(js, "wealth"));
                    CardDrawStat.wealth = wealth;
                }
            }
            string _stat = await CardDrawer.Stat();
            if (!_stat.StartsWith("404:"))
            {
                var stat = Deserializer.ToDictionary(_stat);
                if (stat != null)
                {
                    CardDrawStat.drawCount = ValidationHelper.GetKeyAsInt(stat, "drawCount");
                    CardDrawStat.totalCost = ValidationHelper.GetKeyAsInt(stat, "totalCost");
                    CardDrawStat.totalBonus = ValidationHelper.GetKeyAsInt(stat, "totalBonus");
                    CardDrawStat.cardCount = ValidationHelper.GetKeyAsInt(stat, "cardCount");
                }
            }
            else
            {
                Flower.PlayAnimation("\uEA39", _stat);
            }
        }
        
        private void DisplayOdd()
        {
            odds1.Add(new Odd { Rank = "Mystery", Probability = "0.03%" });
            odds1.Add(new Odd{Rank = "SSR",Probability = "1.50%"});
            odds1.Add(new Odd { Rank = "SR", Probability = "15.00%" });
            odds1.Add(new Odd { Rank = "R", Probability = "29.99%" });
            odds1.Add(new Odd { Rank = "N", Probability = "53.48%" });
            

            odds2.Add(new Odd { Rank = "Mystery", Probability = "0.01%" });
            odds2.Add(new Odd { Rank = "SSR", Probability = "1.50%" });
            odds2.Add(new Odd { Rank = "SR", Probability = "15.00%" });
            odds2.Add(new Odd { Rank = "R", Probability = "30.00%" });
            odds2.Add(new Odd { Rank = "N", Probability = "53.49%" });
            
        }
        private async void StartDraw(string rule)
        {
            cards.Clear();
            string r = await CardDrawer.DrawACard(rule);
            if (!r.StartsWith("404:"))
            {
                var card_list = Deserializer.ToArray(r);
                if (card_list != null)
                {
                    for(int i=0;i<card_list.Count;i++)
                    {
                        var card = Deserializer.ToDictionary(card_list[i].ToString());
                        if (card != null)
                        {
                            string url = ValidationHelper.GetKey(card,"imageUri");
                            cards.Add(new Card
                            {
                                Name= ValidationHelper.GetKey(card, "name"),
                                Order = i.ToString(),
                                ImageUrl = $"https://card.cc98.org{url.Substring(1, url.Length - 1)}",
                                IsFlipped = false
                            });
                        }
                        
                    }
                    RefreshStat();
                }
            }
            else
            {
                Flower.PlayAnimation("\uEA39", r);
            }
            
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
            StartDraw("1");
            await Task.Delay(1000);
            BusyIndicator.Visibility = Visibility.Collapsed;
            ResultViewer.Visibility = Visibility.Visible;
        }

        private async void Drawn_Click(object sender, RoutedEventArgs e)
        {
            BusyIndicator.Visibility = Visibility.Visible;
            ResultViewer.Visibility = Visibility.Collapsed;
            StartDraw("2");
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
                                SingleProperList.ItemsSource = odds1;
                            }
                            else
                            {
                                SingleProperList.ItemsSource = null;
                            }
                            break;
                        case "multi-more":
                            if (MultiProperList.ItemsSource == null)
                            {
                                MultiProperList.ItemsSource = odds1;
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
                                string status = await CardDrawer.DestroyAll();
                                if (status == "1")
                                {
                                    RefreshStat();
                                    Flower.PlayAnimation("\uE930", "分解成功");
                                }
                                else
                                {
                                    Flower.PlayAnimation("\uEA39", "分解失败");
                                }
                            }
                            break;
                        case "ref-stat":
                            RefreshStat();
                            Flower.PlayAnimation("\uE930", "正在刷新数据");
                            break;
                    }
                }
            }
           
            
        }

    }
    public class StatInfoPair
    {
        public required string StatItem { get; set; }
        public int Value { get; set; }
    }
    public partial class CardDrawStat:ObservableObject
    {
        private int _wealth;
        private int _drawCount;
        private int _totalCost;
        private int _totalBonus;
        private int _cardCount;

        public int wealth
        {
            get => _wealth;
            set=>SetProperty(ref _wealth, value);
        }
        public int drawCount
        {
            get => _drawCount;
            set=>SetProperty(ref _drawCount, value);
        }
        public int totalCost
        {
            get => _totalCost;
            set=>SetProperty(ref _totalCost, value);
        }
        public int totalBonus
        {
            get => _totalBonus; 
            set => SetProperty(ref _totalBonus, value);
        }
        public int cardCount
        {
            get => _cardCount; 
            set => SetProperty(ref _cardCount, value);
        }
    }
    public class Odd
    {
        public string Rank { get; set; }
        public string Probability { get; set; }
    }
    public partial class Card: INotifyPropertyChanged
    {
        private string _Order { get; set; }
        private string _ImageUrl { get; set; }
        private bool _IsFlipped { get; set; }
        private string _Name {  get; set; }
        public string Order
        {
            get => _Order;
            set
            {
                if (_Order != value)
                {
                    _Order = value;
                    OnPropertyChanged(nameof(Order));
                }
            }
        }
        public string ImageUrl
        {
            get => _ImageUrl;
            set
            {
                if (_ImageUrl != value)
                {
                    _ImageUrl = value;
                    OnPropertyChanged(nameof(ImageUrl));
                }
            }
        }
        public bool IsFlipped
        {
            get => _IsFlipped;
            set
            {
                if (_IsFlipped != value)
                {
                    _IsFlipped = value;
                    OnPropertyChanged(nameof(IsFlipped));
                }
            }
        }
        public string Name
        {
            get => _Name;
            set
            {
                if (_Name != value)
                {
                    _Name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
