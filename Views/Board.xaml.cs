
using CC98.Kernel;
using CC98.Kernel.ApiScope;
using CC98.Kernel.UserExperience;
using CC98.Objects;
using CC98.Services;
using CC98.Services.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.UI.Controls;
using DevWinUI;
using FluentIcons.Common;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualBasic;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.AppBroadcasting;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Core.Preview;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Board : Page
    {
        public ObservableCollection<SimpleTopicInfo> topics = new();
        public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
        //是否精华帖
        public bool isBest = false;
        public int boardId = 0;
        public Increment increment = new(20);

        public BoardData boardData = new BoardData() { BoardMasters = [],Id=0,BigPaper="",Description="", Name = "版面", TodayCount = 9898, TopicCount = 9898 };
        public Board()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var args = e.TryGetParameter<int>();
            boardId = args;
            BoardSymbol.Symbol=BoardIcon.GetSymbol(boardId,"");
            await GetData();
            await LoadTopics();
        }
        
        private async Task GetData()
        {
            string boardDataUrl = ApiEndpoints.Board.BoardInfo(boardId);
            var boardDataResult = await RequestSender.Fetch<BoardData>(boardDataUrl);
            if (!boardDataResult.IsSuccess || boardDataResult.Data == null) 
            {
                Flower.Play("\uEA39", boardDataResult.Message);
                return;
            }
            var data= boardDataResult.Data;
            boardData.Id = data.Id;
            boardData.Name = data.Name;
            boardData.Description = data.Description;
            boardData.BigPaper = data.BigPaper;
            boardData.BoardMasters = data.BoardMasters;
            boardData.TopicCount = data.TopicCount;
            boardData.TodayCount = data.TodayCount;   
        }

        private async Task<bool> LoadTopics()
        {
            string topicUrl = ApiEndpoints.Board.TopicList(isBest, boardId, increment.startIndex);
            if (isBest)
            {
                var result = await RequestSender.Fetch<BoardBest>(topicUrl);
                if (result.IsNotValid)
                {
                    Flower.Play(FlowStatus.Fail, result.Message);
                    return false;
                }
                var bests= result.Data?.Topics;
                increment.hasMore= bests.Count == increment.pageSize;
                topics.AddRange(bests);
                return true;
            }
            else
            {
                var topicResult = await RequestSender.Fetch<List<SimpleTopicInfo>>(topicUrl);
                if (topicResult.IsNotValid)
                {
                    Flower.Play(FlowStatus.Fail, topicResult.Message);
                    return false;
                }
                var data = topicResult.Data;
                increment.hasMore = data.Count == increment.pageSize;
                topics.AddRange(data);
                return true;
            }
        }


        private void TileContent_Click(object sender, RoutedEventArgs e)
        {
            var h = sender as HyperlinkButton;
            var t = h?.DataContext as SimpleTopicInfo;
            if (t == null) return;
            var param = new TopicNavigationInfo { TopicId = t.Id };
            Frame.Navigate(typeof(Topic), param);
        }

        private async void Gooey_Click(object sender, RoutedEventArgs e)
        {
            var s = sender as GooeyButtonItem;
            if (s != null)
            {
                var _tag = s.Tag;
                if (_tag is string tag)
                {
                    switch (tag)
                    {
                        case "Send":
                            var param = new EditorNavigationInfo
                            {
                                EditorMode=EditorMode.DraftNewTopic,
                                BoardId=boardId
                            };
                            Frame.Navigate(typeof(UBBEditor), param);
                            break;
                        case "Pin":
                            string url = ApiEndpoints.Board.EditFocusBoards(boardId);
                            var content = new StringContent("", Encoding.UTF8, "application/json");
                            var result = await RequestSender.Put(url, content);
                            if (!result.IsSuccess)
                            {
                                //
                                return;
                            }
                            var i = new NavigationItem
                            {
                                IconSymbol = BoardIcon.GetSymbol(boardId, boardData.Name),
                                Name = boardData.Name,
                                IsEditable = true,
                                Tag = boardId.ToString()
                            };
                            Messenger.Instance.AddNavigationItem(i);
                            break;
                        case "Best":
                            //GooeyGroup.Visibility = Visibility.Collapsed;
                            //BackFromBest.Visibility = Visibility.Visible;
                            increment.Clear();
                            topics.Clear();
                            isBest = true;
                            
                            await LoadTopics();
                            break;
                    }

                }
            }

        }

        private async void BackFromBest_Click(object sender, RoutedEventArgs e)
        {
            //BackFromBest.Visibility = Visibility.Collapsed;
            //GooeyGroup.Visibility = Visibility.Visible;
            isBest = false;
            increment.Clear();
            topics.Clear();
            await LoadTopics();
        }

        

        private async void TopicRepeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
        {
            await increment.LoadMore(args.Index,LoadTopics);
        }
    }
}