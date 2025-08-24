using CCkernel;
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
        private static PasswordVault vault = new PasswordVault();
        public ObservableCollection<Card> cards = new ObservableCollection<Card>();
        public ObservableCollection<StatInfoPair> stats = new();
        public ObservableCollection<Odd> odds1 = new();
        public ObservableCollection<Odd> odds2 = new();
        public Game()
        {
            this.InitializeComponent();
            DisplayOdd();
            RefreshStat();
            StatList.ItemsSource = stats;
            CardList.ItemsSource = cards;
            
        }
        private async void RefreshStat()
        {
            stats.Clear();
            StatInfoPair wealth_info = new StatInfoPair { StatItem = "财富值", Value = 0 };
            string ProfileUrl = "https://api.cc98.org/me";
            string ProfileText = await RequestSender.SimpleRequest(ProfileUrl);
            if (!ProfileText.StartsWith("404:"))
            {
                var js = Deserializer.ToDictionary(ProfileText);
                if (js != null)
                {
                    int wealth = Convert.ToInt32(ValidationHelper.GetKey(js, "wealth"));
                    wealth_info.Value = wealth;
                }
            }
            stats.Add(wealth_info);
            string _stat = await CardDrawer.Stat();
            if (!_stat.StartsWith("404:"))
            {
                var stat = JsonConvert.DeserializeObject<StatInfo>(_stat);
                if (stat != null)
                {
                    stats.Add(new StatInfoPair { StatItem = "抽卡次数", Value = stat.drawCount });
                    stats.Add(new StatInfoPair { StatItem = "花费", Value = stat.totalCost });
                    stats.Add(new StatInfoPair { StatItem = "收益", Value = stat.totalBonus });
                    stats.Add(new StatInfoPair { StatItem = "卡片总数", Value = stat.cardCount });
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
                var card_list = JsonConvert.DeserializeObject<List<CardData>>(r);
                if (card_list != null)
                {
                    for(int i=0;i<card_list.Count;i++)
                    {
                        string url = card_list[i].imageUri;
                        cards.Add(new Card
                        {
                            Order = i.ToString(),
                            ImageUrl = $"https://card.cc98.org{url.Substring(1,url.Length-1)}",
                            IsFlipped = false
                        });
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
                            break;
                    }
                }
            }
           
            
        }

    }
    public class StatInfoPair()
    {
        public required string StatItem { get; set; }
        public int Value { get; set; }
    }
    public class StatInfo()
    {
        public int drawCount { get; set; }
        public int totalCost { get; set; }
        public int totalBonus { get;set; }
        public int cardCount { get;set; }
    }
    public class Odd()
    {
        public string Rank { get; set; }
        public string Probability { get; set; }
    }
    public class Card(): INotifyPropertyChanged
    {
        private string _Order { get; set; }
        private string _ImageUrl { get; set; }
        private bool _IsFlipped { get; set; }
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
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
