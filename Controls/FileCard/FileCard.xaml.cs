using CC98.Share.Controls;
using CC98.Share.Controls.Primitives;
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
using UbbRender.Common;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98.Share.Controls
{
    public sealed partial class FileCard : UserControl
    {
        public FileCard()
        {
            InitializeComponent();
        }
        #region 依赖属性

        public static readonly DependencyProperty SrcProperty =
            DependencyProperty.Register(
                "Src",
                typeof(string),
                typeof(FileCard),
                new PropertyMetadata("",OnSrcChanged));
        public string Src
        {
            get => (string)GetValue(SrcProperty);
            set => SetValue(SrcProperty, value);
        }

        #endregion
        #region 加载事件
        private static void OnSrcChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is not FileCard fileCard)return;
            fileCard.LoadFile();
        }

        public event EventHandler<MediaClickEventArgs> DownloadRequested;

        private void Root_Click(object sender, RoutedEventArgs e)
        {
            DownloadRequested?.Invoke(this, new MediaClickEventArgs(Src, MediaType.File));
        }
        private void LoadFile()
        {
            string extension = Path.GetExtension(Src)?.ToUpper() ?? "未知类型";
            extension = extension.Replace(".", "");
            string fileName = ExtractFileNameFromUrl(Src);
            ExtensionNameBox.Text = extension;
            FileNameBox.Text = fileName;
            TypeIcon.Symbol = GetFileTypeIcon(fileName);
        }
        #endregion


        #region 辅助函数
        private static FluentIcons.Common.Symbol GetFileTypeIcon(string fileName)
        {
            var extension = Path.GetExtension(fileName)?.ToLower();
            return extension switch
            {
                ".pdf" => FluentIcons.Common.Symbol.DocumentPdf,
                ".xls" or ".xlsx" => FluentIcons.Common.Symbol.Table,
                ".ppt" or ".pptx" => FluentIcons.Common.Symbol.DocumentFlowchart,
                "zip" or ".rar" => FluentIcons.Common.Symbol.FolderZip,
                "mp3" or ".wav" or "m4a" => FluentIcons.Common.Symbol.MusicNote1,
                "mp4" or ".avi" => FluentIcons.Common.Symbol.Video,
                "jpg" or ".jpeg" or ".png" or ".gif" or "webp" => FluentIcons.Common.Symbol.Image,
                _ => FluentIcons.Common.Symbol.Document
            };
        }


        /// <summary>
        /// 从URL中提取文件名
        /// </summary>
        private static string ExtractFileNameFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var fileName = Path.GetFileName(uri.LocalPath);
                // 如果文件名无效，生成默认文件名
                if (string.IsNullOrEmpty(fileName) || !fileName.Contains('.'))
                {
                    fileName = $"CC{DateTime.Now:MMdd_HHmm}.pdf";
                }

                return fileName;
            }
            catch
            {
                return $"CC{DateTime.Now:MMdd_HHmm}.pdf";
            }
        }
        #endregion

        
    }
}
