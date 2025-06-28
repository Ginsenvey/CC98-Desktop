using CCkernel;
using CommunityToolkit.Labs.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using static App3.Section;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App3
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Focus : Page
    {
        public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
        public ObservableCollection<ToggleBoardInfo> boards=new ObservableCollection<ToggleBoardInfo>();
        
        public Focus()
        {
            this.InitializeComponent();
            if (Set.Values.ContainsKey("CustomBoards"))
            {
                if (Set.Values["CustomBoards"].ToString() != "0")
                {
                    memory = JsonConvert.DeserializeObject<Dictionary<string, string>>(Set.Values["CustomBoards"].ToString());
                }
                
                
            }
            else
            {
                //初始化本地缓存
                Set.Values["CustomBoards"] = "0";
            }
            Focuses.ItemsSource = boards;
            GetFocusBoards();
        }
        
        private async void GetFocusBoards()
        {
            string ProfileUrl = "https://api.cc98.org/me";
            var ProfileRes = await CCloginservice.client.GetAsync(ProfileUrl);
            if (ProfileRes.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string ProfileText = await ProfileRes.Content.ReadAsStringAsync();
                var js = JsonConvert.DeserializeObject<Dictionary<string, object>>(ProfileText);
                var boards = JsonConvert.DeserializeObject<JArray>(js["customBoards"].ToString());
                Dictionary<string, string> CustomBoards = new();
                foreach (var b in boards)
                {
                    AddBoards(b.ToString());
                }

                

            }
        }
        public Dictionary<string, string> memory = new();
        private async void AddBoards(string BoardId)
        {
            //先判断本地存储是否有此板块
            if (!memory.ContainsKey(BoardId))
            {
                string BoardUrl = "https://api.cc98.org/board/" + BoardId;
                try
                {
                    var BoardRes = await CCloginservice.client.GetAsync(BoardUrl);
                    if (BoardRes.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string BoardText = await BoardRes.Content.ReadAsStringAsync();
                        if (!string.IsNullOrEmpty(BoardText))
                        {
                            var js = JsonConvert.DeserializeObject<Dictionary<string, object>>(BoardText);
                            if (js != null)
                            {
                                string name = js["name"].ToString();
                                memory.Add(BoardId, name);
                                boards.Add(new ToggleBoardInfo {Name=BoardId,Sort=name,IsSelected=false});
                                string boardjsontext = JsonConvert.SerializeObject(memory);
                                Set.Values["CustomBoards"] = boardjsontext;
                            }
                        }
                    }
                }
                catch
                {

                }
            }
            else
            {
                //如果本地存储有此板块，则直接添加
                boards.Add(new ToggleBoardInfo { Name = memory[BoardId], Sort = BoardId,IsSelected=false });
            }
        }

        private void ExclusiveButton_Click(object sender, RoutedEventArgs e)
        {
            var t=sender as ToggleButton;
            if (t != null)
            {
                var i = t.DataContext as ToggleBoardInfo;
                if (i!=null)
                {
                    string sort = i.Sort;
                    foreach (var board in boards)
                    {
                        if (board.Sort != sort)
                        {
                            board.IsSelected = false;
                        }
                    }
                }
            }
            
        }

        private void ExclusiveButton_Checked(object sender, RoutedEventArgs e)
        {
            var t=sender as ToggleButton;
            
            if (t != null)
            {
                var i = t.DataContext as ToggleBoardInfo;
                if (i != null)
                {
                    if (i.IsSelected== false)
                    {
                        i.IsSelected = true ;
                    }
                    else
                    {
                        i.IsSelected = true;
                    }
                }
                
                
            }
            
            
        }
    }

    public class ToggleBoardInfo:INotifyPropertyChanged
    {
        private bool _IsSelected;
        private string _name;
        private string _sort;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public bool IsSelected
        {
            get => _IsSelected;
            set => SetProperty(ref _IsSelected, value);
        }

        public string Sort
        {
            get => _sort;
            set => SetProperty(ref _sort, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
