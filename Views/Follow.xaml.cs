using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using System.Net.Http;
using System.Net;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

using CC98.Kernel;
using CommunityToolkit.Mvvm.ComponentModel;
using CC98.Kernel.ApiScope;
using CC98.Objects;
using DevWinUI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Follow : Page
    {
        public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
        public ObservableCollection<Friend> friends = new ObservableCollection<Friend>();
        public Follow()
        {
            this.InitializeComponent();
            Collection.ItemsSource = friends;
        }

        
        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // 获取传递的参数
            var parameter = e.Parameter as string;

            if (!string.IsNullOrEmpty(parameter))
            {
                Set.Values["CurrentFriendType"] = parameter;
                if (parameter == "follower")
                {
                    FriendType.Text = "粉丝";
                }
                else if(parameter == "followee")
                {
                    FriendType.Text = "关注";
                }  
            }
            string type = ValidationHelper.GetValue(Set, "CurrentFriendType");
            if(type!="0")
            {
                LoadFriend(type, 0);
            }
            

        }
        

        private async void LoadFriend(string type, int start)
        {
            //获取好友ID列表
            string friendListUrl = ApiEndpoints.User.FreiendList(type, start);
            var friendIdsResult = await RequestSender.Fetch<List<int>>(friendListUrl);
            //处理第一层异常
            if (!friendIdsResult.IsSuccess || friendIdsResult.Data == null)
            {
                //报错
                return;
            }
            var ids = friendIdsResult.Data;
            if (ids.Count == 0) return;

            var param = string.Join("&", ids.Select(id => $"id={id}"));
            string userInfoUrl = ApiEndpoints.User.UserInfoList(param);

            // 获取好友详情
            var friendsResult = await RequestSender.Fetch<List<Friend>>(userInfoUrl);
            if (!friendsResult.IsSuccess || friendsResult.Data == null)
            {
                //报错
            }
            friends.AddRange(friendsResult.Data); 
        }

        private void TileContent_Click(object sender, RoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            if(h!= null)
            {
                var f = h?.DataContext as Friend;
                if (f != null)
                {
                    var param = new Dictionary<string, string>()
                    {
                        {"Mode","Others" },
                        {"UserId", f.uid}
                    };
                    
                    Frame.Navigate(typeof(Profile), param);
                }
            }
        }
        public int history = 0;
        private void Collection_Loaded(object sender, RoutedEventArgs e)
        {
            Collection.ElementPrepared += (s, e) =>
            {
                if (Collection.ItemsSource != null)
                {
                    int current = e.Index;
                    
                    if (current > 0 && (current + 1) % 10 == 0 && current > history)
                    {
                        
                        history = current;
                        LoadFriend(ValidationHelper.GetValue(Set,"CurrentFriendType"), current + 1);

                    }
                }
            };
        }

        private async void Follow_Click(object sender, RoutedEventArgs e)
        {
            var m = sender as MenuFlyoutItem;
            if (m != null)
            {
                var _tag = m.Tag;
                if(_tag is string tag)
                {
                    string restext = await RequestSender.Follow("0", tag);
                    if (restext == "1")
                    {
                        friends.Clear();
                        LoadFriend(ValidationHelper.GetValue(Set, "CurrentFriendType"), 0);
                        history = 0;
                        Flower.PlayAnimation("\uE930", "已取消关注");
                    }
                    else
                    {
                        Flower.PlayAnimation("\uEA39", "取消关注失败");
                    }
                }
            }
        }

        private void Chat_Click(object sender, RoutedEventArgs e)
        {
            var m= sender as MenuFlyoutItem;
            if (m != null)
            {
                var f = m?.DataContext as Friend;
                if (f != null)
                {
                    var c=new TargetUserInfo { TargetUserId=f.uid,TargetUserName=f.name ,PortraitUrl=f.url};
                    var p = new Dictionary<string, object>()
                    {
                        {"Type","1" },
                        {"Info",c }
                    };
                    Frame.Navigate(typeof(Message), p);
                }
            }
        }
    }
  
    
}
