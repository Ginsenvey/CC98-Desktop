using CC98.Objects;
using CC98.Services;
using CC98.Services.Extensions;
using CommunityToolkit.WinUI.Controls;
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
        public MessageNavigationInfo NavigationInfo { get; set; }=new MessageNavigationInfo();
        public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
        public GlobalService GlobalService =GlobalService.Instance;
        public Message()
        {
            this.InitializeComponent();
        }
        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (GlobalService.ShouldReplaceNavigationArgs)
            {
                if(GlobalService.NavigationAnchor is int targetIndex)
                {
                    NaviBar.SelectedItem = NaviBar.Items[targetIndex];
                }
                else
                {
                    NaviBar.SelectedItem = NaviBar.Items[0];
                }
                return;
            }
            var args = e.TryGetParameter<MessageNavigationInfo>();
            if (args == null) return;
            NavigationInfo = args;
            NaviBar.SelectedItem = NaviBar.Items[0];
        }
        
        private void NaviBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            var bar = NaviBar.SelectedItem as SelectorBarItem;
            var selected=NaviBar.Items.IndexOf(bar);
            GlobalService.Instance.NavigationAnchor = selected;
            if (bar?.Tag is not string tag) return;
            switch (tag)
            {
                case "Chat":
                    MsgFrame.Navigate(typeof(Chat), NavigationInfo);
                    MsgCount.Text = $"{GlobalService.MessageCount}条未读信息";
                    break;
                case "System":
                    MsgFrame.Navigate(typeof(NoticePage), NoticeType.System);
                    MsgCount.Text = $"{GlobalService.SystemCount}条未读信息";
                    break;
                case "Reply":
                    MsgFrame.Navigate(typeof(NoticePage), NoticeType.Reply);
                    MsgCount.Text = $"{GlobalService.ReplyCount}条未读信息";
                    break;
                case "At":
                    MsgFrame.Navigate(typeof(NoticePage), NoticeType.At);
                    MsgCount.Text = $"{GlobalService.AtCount}条未读信息";
                    break;
            }
           
        }

      
    }
}
