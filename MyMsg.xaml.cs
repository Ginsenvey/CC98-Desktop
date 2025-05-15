using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App3
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MyMsg : Page
    {
        public ObservableCollection<Msg> Msgs = new ObservableCollection<Msg>()
        {
            
        };
        public MyMsg()
        {
            this.InitializeComponent();
            
        }

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // 获取传递的参数
            var parameter = e.Parameter as string;

            if (parameter != null)
            {
                GetDialogs(parameter,"0");
                currentuid = parameter;
            }
            else
            {

            }
        }
        public string currentuid = "";
        private async void GetDialogs(string uid,string start)
        {
            string murl = "https://api.cc98.org/message/user/" + uid + "?from="+start+"&size=10";
            var res = await MainWindow.loginservice.client.GetAsync(murl);
            if (res.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string restext = await res.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(restext))
                {
                    var list = JsonConvert.DeserializeObject<JArray>(restext);
                    if (list != null)
                    {
                        history += 10; 
                        foreach (var c in list)
                        { 
                            var js = JsonConvert.DeserializeObject<Dictionary<string, object>>(c.ToString());
                            string msgid = js["id"].ToString();//消息ID
                            bool isMe = js["receiverId"].ToString() == uid;
                            string text = js["content"].ToString();
                            string time = js["time"].ToString();
                            Msgs.Add(new Msg
                            {
                                msgid = msgid,
                                text = text,
                                time = time,
                                uid = isMe
                            });
                        }
                        var reversedlist = Msgs.Reverse();
                        MessagesList.ItemsSource = reversedlist;
                    }
                }
            }
            
        }
        public int history = 0;
        private void MoreMsg_RefreshRequested(RefreshContainer sender, RefreshRequestedEventArgs args)
        {
            if (currentuid != "")
            {
                GetDialogs(currentuid, history.ToString());
            }
            
        }
    }
    public class Msg : INotifyPropertyChanged
    {
        private string _time;
        private string _text;
        private bool _uid;
        private string _msgid;
        public string time
        {
            get { return _time; }
            set
            {
                if (_time != value)
                {
                    _time = value;
                    OnPropertyChanged(nameof(time));
                }
            }
        }

        public string text
        {
            get { return _text; }
            set
            {
                if (_text != value)
                {
                    _text = value;
                    OnPropertyChanged(nameof(text));
                }
            }
        }

        public bool uid
        {
            get { return _uid; }
            set
            {
                if (_uid != value)
                {
                    _uid = value;
                    OnPropertyChanged(nameof(uid));
                }
            }
        }
        public string msgid
        {
            get { return _msgid; }
            set
            {
                if (_msgid != value)
                {
                    _msgid = value;
                    OnPropertyChanged(nameof(msgid));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public class AlignmentConverter : IValueConverter
    {
        // Convert（正向转换：数据源 -> 界面）
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ?
                HorizontalAlignment.Right :
                HorizontalAlignment.Left;
        }

        // ConvertBack（逆向转换：界面 -> 数据源）
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            // 如果不需要双向绑定，可以直接返回默认值
            return DependencyProperty.UnsetValue;

            // 或者抛出异常（推荐做法）
            // throw new NotImplementedException();
        }
    }

    public class BackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ?
                new SolidColorBrush(Color.FromArgb(255, 210, 179, 174)) :
                new SolidColorBrush(Color.FromArgb(255, 212, 238, 253));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public class DateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return ((DateTime)value).ToString("HH:mm");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
