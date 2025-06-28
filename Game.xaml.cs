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
using Windows.Storage;
using Windows.Security.Credentials;
using CCkernel;
using HtmlAgilityPack;
using System.Net.Http;
using Windows.Graphics.Imaging;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Xml.Linq;
using DevWinUI;
using System.ComponentModel;
using static System.Net.Mime.MediaTypeNames;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

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
        public ObservableCollection<Odd> odds1 = new();
        public ObservableCollection<Odd> odds2 = new();
        public Game()
        {
            this.InitializeComponent();
            Prepare();
            DisplayOdd();
            
            CardList.ItemsSource = cards;
        }
        
        private void DisplayOdd()
        {
            odds1.Add(new Odd { Rank = "Mystery", Probability = "0.03%" });
            odds1.Add(new Odd{Rank = "SSR",Probability = "1.50%"});
            odds1.Add(new Odd { Rank = "SR", Probability = "15.00%" });
            odds1.Add(new Odd { Rank = "R", Probability = "29.99%" });
            odds1.Add(new Odd { Rank = "N", Probability = "53.48%" });
            SingleProperList.ItemsSource = odds1;

            odds2.Add(new Odd { Rank = "Mystery", Probability = "0.01%" });
            odds2.Add(new Odd { Rank = "SSR", Probability = "1.50%" });
            odds2.Add(new Odd { Rank = "SR", Probability = "15.00%" });
            odds2.Add(new Odd { Rank = "R", Probability = "30.00%" });
            odds2.Add(new Odd { Rank = "N", Probability = "53.49%" });
            MultiProperList.ItemsSource = odds2;
        }
        private async void Prepare()
        {
            PasswordCredential credential = vault.Retrieve("CC98", "User");
            string name = Set.Values["Me"] as string;
            string password = credential.Password;
            
            string r = await CCloginservice.DeepAuthService(name, password);
            if (r == "1")
            {
                string cardurl = "https://card.cc98.org/Account/LogOn?returnUrl=https%3A%2F%2Fcard.cc98.org%2F";
                var res = await CCloginservice.client.GetAsync(cardurl);
                var callback = res.Headers.Location;

                var connect = await CCloginservice.client.GetAsync(callback);
                var callback1 = connect.Headers.Location;
                var res1 = await CCloginservice.client.GetAsync(callback1);

                string content = await res1.Content.ReadAsStringAsync();
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(content);
                string xpath = "//input[@name='__RequestVerificationToken']";
                var node = doc.DocumentNode.SelectSingleNode(xpath);
                if (node != null)
                {
                    string token = node.GetAttributeValue("value", "");
                    var parameters = new List<KeyValuePair<string, string>>();
                    parameters.Add(new KeyValuePair<string, string>("Scopes", "openid"));

                    parameters.Add(new KeyValuePair<string, string>("IsConsent", "true"));

                    parameters.Add(new KeyValuePair<string, string>("Scopes", "profile"));

                    parameters.Add(new KeyValuePair<string, string>("__RequestVerificationToken", token));

                    parameters.Add(new KeyValuePair<string, string>("RememberConsent", "false"));
                    //发送含有重复键值表单的方法
                    var postdata = new FormUrlEncodedContent(parameters);
                    var res2 = await CCloginservice.client.PostAsync(callback1, postdata);
                    string openurl = "https://openid.cc98.org" + res2.Headers.Location.ToString();
                    var res3 = await CCloginservice.client.GetAsync(openurl);
                    string info = await res3.Content.ReadAsStringAsync();
                    HtmlDocument infodoc = new HtmlDocument();
                    infodoc.LoadHtml(info);
                    var id_token = infodoc.DocumentNode.SelectSingleNode("//input[@name='id_token']")?.GetAttributeValue("value", "");
                    var code = infodoc.DocumentNode.SelectSingleNode("//input[@name='code']")?.GetAttributeValue("value", "");
                    var scope = infodoc.DocumentNode.SelectSingleNode("//input[@name='scope']")?.GetAttributeValue("value", "");
                    var state = infodoc.DocumentNode.SelectSingleNode("//input[@name='state']")?.GetAttributeValue("value", "");
                    var session_state = infodoc.DocumentNode.SelectSingleNode("//input[@name='session_state']")?.GetAttributeValue("value", "");
                    string drawurl = "https://card.cc98.org/signin-cc98";
                    var pack = new Dictionary<string, string>()
                    {
                        {"code",code},
                        {"scope",scope},
                        {"state",state},
                        {"id_token",id_token},
                        {"session_state",session_state }
                    };
                    var packdata = new FormUrlEncodedContent(pack);
                    var signres = await CCloginservice.client.PostAsync(drawurl, packdata);
                    string recall = "https://card.cc98.org/Account/LogOnCallback?returnUrl=https%3A%2F%2Fcard.cc98.org%2F";
                    var recallres = await CCloginservice.client.GetAsync(recall);
                    
                }
                else
                {

                }

            }
        }

        private async Task<string> StartDraw(string mode)
        {
            CardList.ItemsSource = null;
            cards.Clear();
            CardList.ItemsSource = cards;
            string single = "https://card.cc98.org/Draw/Detail/"+mode;
            //mode为1表示单抽，2为十一连抽
            var singleRes = await CCloginservice.client.GetAsync(single);
            string singleContent = await singleRes.Content.ReadAsStringAsync();
            HtmlDocument singleDoc = new HtmlDocument();
            singleDoc.LoadHtml(singleContent);
            var tokennode = singleDoc.DocumentNode.SelectSingleNode("//input[@name='__RequestVerificationToken']");
            if (tokennode != null)
            {
                string singletoken = tokennode.GetAttributeValue("value", "");
                var singlepack = new Dictionary<string, string>()
                        {
                            {"__RequestVerificationToken",singletoken},
                            {"X-Requested-With","XMLHttpRequest" }
                        };
                var singlepackdata = new FormUrlEncodedContent(singlepack);
                var drawres = await CCloginservice.client.PostAsync("https://card.cc98.org/Draw/Run/"+mode, singlepackdata);
                if (drawres.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string drawContent = await drawres.Content.ReadAsStringAsync();
                    HtmlDocument drawDoc = new HtmlDocument();
                    drawDoc.LoadHtml(drawContent);
                    string xpath = "//img[@class='card-img' and @alt='卡面图案']";
                    List<string> ImageList = ExtractNode(drawDoc, xpath);
                    if (ImageList != null)
                    {
                        for (int i = 0; i < ImageList.Count; i++)
                        {
                            Card card = new Card()
                            {
                                Order = i.ToString(),
                                ImageUrl = "https://card.cc98.org" +ImageList[i],
                                IsFlipped = false
                            };
                            cards.Add(card);
                        }
                        return "1";
                    }
                    else
                    {
                        return "0";
                    }
                }
                else
                {
                    return "0";
                }

            }
            else
            {
                return "0";
            }
        }
        private List<string> ExtractNode(HtmlDocument doc,string xpath)
        {
            HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes(xpath);

            // 处理不同结果数量
            if (nodes == null)
            {
                return null;
            }
            else
            {
                if (nodes.Count == 0)
                {
                    return null;
                }
                else if(nodes.Count == 1)
                {
                    var singleNode = nodes[0];
                    return new List<string> { singleNode.GetAttributeValue("src", "") };
                }
                else
                {
                    var list = new List<string>();
                    foreach (var node in nodes)
                    {
                        list.Add(node.GetAttributeValue("src", ""));
                    }
                    return list;
                }
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
            string draw=await StartDraw("1");
            await Task.Delay(1000);
            BusyIndicator.Visibility = Visibility.Collapsed;
            ResultViewer.Visibility = Visibility.Visible;
        }

        private async void Drawn_Click(object sender, RoutedEventArgs e)
        {
            BusyIndicator.Visibility = Visibility.Visible;
            ResultViewer.Visibility = Visibility.Collapsed;
            string draw = await StartDraw("2");
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
