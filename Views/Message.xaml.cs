using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Contacts;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Devices;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98
{
    public sealed partial class Message : Page
    {
        public Dictionary<string,object> param=new Dictionary<string,object>();
        public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
        public Message()
        {
            this.InitializeComponent();
        }
        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // 获取传递的参数
            var p = e.Parameter as Dictionary<string,object>;

            if (p != null)
            {
                param = p;
                ChatMsg.IsSelected = true;
            }
        }
        
        private void NaviBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            var bar = NaviBar.SelectedItem as SelectorBarItem;
            if (bar != null)
            {
                var _tag = bar.Tag;
                if(_tag is string tag)
                {
                    if (tag == "0")//私信
                    {
                        MsgFrame.Navigate(typeof(Chat), param);
                    }
                    else if (tag == "1")//系统通知
                    {
                        MsgFrame.Navigate(typeof(NoticeMsg), "system");
                    }
                    else if (tag == "2")//回复我的
                    {
                        MsgFrame.Navigate(typeof(NoticeMsg), "reply");
                    }
                } 
                
            }
        }

      
    }
}
