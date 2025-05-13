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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App3
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Section : Page
    {
        public ObservableCollection<AllSection> allSections;
        public Section()
        {
            this.InitializeComponent();
            allSections = new ObservableCollection<AllSection>()
            {
                
            };
            SectionPresenter.ItemsSource = allSections;

            GetAllSection();
        }
        private async void GetAllSection()
        {
            string SectionUrl = "https://api.cc98.org/Board/all";
            try
            {
                var SectionRes = await MainWindow.loginservice.client.GetAsync(SectionUrl);
                if (SectionRes.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string SectionText = await SectionRes.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(SectionText))
                    {
                        var SectionArray = JsonConvert.DeserializeObject<JArray>(SectionText);
                        if (SectionArray != null)
                        {
                            if (SectionArray.Count > 0)
                            {
                                foreach(var section in SectionArray)
                                {
                                    List<BoardInfo> boardinfo = new();
                                    var info = JsonConvert.DeserializeObject<Dictionary<string, object>>(section.ToString());
                                    string name = info["name"].ToString();
                                    string mastertext = info["masters"].ToString();
                                    var boards = JsonConvert.DeserializeObject<JArray>(info["boards"].ToString());
                                    foreach(var board in boards)
                                    {
                                        var js = JsonConvert.DeserializeObject<Dictionary<string,object>>(board.ToString());
                                        boardinfo.Add(new BoardInfo { BoardName = js["name"].ToString(), BoardId = js["id"].ToString() });
                                    }
                                    allSections.Add(new AllSection { SectionName = name,Boards = boardinfo });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void BoardButton_Click(object sender, RoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            var tag = h.Tag as string;//当前绑定状态下，h没有DataContext.只能使用tag.
            if(tag != null)
            {
                Frame.Navigate(typeof(Board),tag); 
            }
        }
    }
    public class AllSection
    {
        public string SectionName
        {
            get; set;
        }
        public List<BoardInfo> Boards
        {
            get; set;
        }
    }
    public class BoardInfo
    {
        public string BoardName { get; set; }
        public string BoardId { get; set; }
    }
}
