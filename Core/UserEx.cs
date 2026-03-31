using CC98.Kernel;
using FluentIcons.Common;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace CC98.Kernel.UserExperience//用户体验模型，包括:版面图标；语义搜索；图像解析。
{
    public static class BoardIcon
    {
        public static FluentIcons.Common.Symbol GetSymbol(int Id, string Name)
        {
            if (BoardIcon.Icons.ContainsKey(Id))
            {
                return BoardIcon.Icons[Id];
            }
            else
            {
                if (!string.IsNullOrEmpty(Name))
                {
                    if (Name.Contains("答疑"))
                    {
                        return FluentIcons.Common.Symbol.Teaching;
                    }
                    else if (Name.Contains("院"))
                    {
                        return FluentIcons.Common.Symbol.ChartPerson;
                    }

                }
                return FluentIcons.Common.Symbol.Tag;
            }
        }
        private static readonly Dictionary<int, FluentIcons.Common.Symbol> Icons = new()
        {
            {758,Symbol.LeafOne},
            {182,Symbol.Heart },
            {184,Symbol.ChatHelp},
            {68,Symbol.Library },
            {581,Symbol.BeakerEdit },
            {102,Symbol.HatGraduation },
            {304,Symbol.Translate },
            {263 ,Symbol.TaskList},
            {105,Symbol.Code },
            {749,Symbol.ReadingList },
            {100,Symbol.InfoSparkle},
            {777,Symbol.Shield },
            {357 ,Symbol.BoardSplit},
            {459,Symbol.CalendarWorkWeek },
            {515,Symbol.BuildingTownhouse },
            {235,Symbol.Agents },
            {782,Symbol.ArrowTrending },
            {180,Symbol.PhoneDesktop },
            {30,Symbol.AnimalPawPrint },
            {760,Symbol.Bookmark },
            {26,Symbol.BookmarkMultiple},
            {25,Symbol.MusicNote2 },
            {91,Symbol.Games },
            {115,Symbol.LeafTwo},
            {744,Symbol.MoviesAndTv},
            {788,Symbol.DriveTrain},
            {43,Symbol.ShoppingBag},
            {562,Symbol.ShoppingBagAdd},
            {569,Symbol.AgentsAdd},
            {764,Symbol.ShoppingBagArrowLeft },
            {114,Symbol.HeartBroken},
            {81,Symbol.WeatherMoon },
            {152,Symbol.HeartPulse},
            {135,Symbol.WeatherSunny },
            {15,Symbol.Sport },
            {226,Symbol.Toolbox},
            {258,Symbol.AnimalCat },
            {173,Symbol.CameraSparkles},
            {353,Symbol.Sparkle},
            {229,Symbol.FoodPizza},
            {261,Symbol.LeafThree},
            {315,Symbol.Album},

        };
    }
    public static class UrlEx
    {
        public static bool IsWebUrl(string url)
        {
            return url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                   url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsLocalPath(string path)
        {
            return path.StartsWith("ms-appx:///", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("ms-appdata:///", StringComparison.OrdinalIgnoreCase) ||
                   Path.IsPathRooted(path) || // 绝对路径
                   !Path.HasExtension(path);   // 资源路径
        }

        public static bool IsWebUri(Uri uri)
        {
            return uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ||
                   uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsLocalUri(Uri uri)
        {
            return uri.Scheme.Equals("ms-appx", StringComparison.OrdinalIgnoreCase) ||
                   uri.Scheme.Equals("ms-appdata", StringComparison.OrdinalIgnoreCase) ||
                   uri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase) ||
                   uri.ToString().Contains("/Assets/");
        }
        public static Uri ChangeExtension(Uri originalUri, string newExtension)
        {
            // 获取原始URI的绝对路径（对于ms-appx协议，这类似于“/Assets/Images/image.gif”）
            string absolutePath = originalUri.AbsolutePath;
            // 检查是否有查询字符串，如果有需要保留（但通常ms-appx资源URI没有查询字符串）
            string query = originalUri.Query;
            // 修改文件路径：去掉原扩展名，加上新扩展名
            int lastDotIndex = absolutePath.LastIndexOf('.');
            int lastSlashIndex = absolutePath.LastIndexOf('/');
            // 确保我们只修改文件名的扩展名部分（即最后一个点号，且该点号在最后一个斜杠之后）
            if (lastDotIndex > lastSlashIndex)
            {
                absolutePath = absolutePath[..lastDotIndex] + newExtension;
            }
            else
            {
                // 如果没有找到点号，或者点号不在文件名中（即在目录名中），则直接在后面添加扩展名
                absolutePath += newExtension;
            }
            // 重新构建URI，使用原始协议（ms-appx）和新的路径
            // 注意：原始URI的主机部分（如果有）通常为空，因为ms-appx协议是本地资源
            string newUriString = $"{originalUri.Scheme}://{originalUri.Host}{absolutePath}{query}";
            return new Uri(newUriString);
        }
        public static async Task<string> LocateEmoji(string path)
        {
            //应对表情包有两种格式的情况
            if (await IsResourceExistsAsync(path))
            {
                return path;
            }
            else
            {
                Uri uri = new(path);
                return ChangeExtension(uri, ".gif").ToString();
            }
        }
        public static async Task<bool> IsResourceExistsAsync(string path)
        {
            try
            {
                var uri = new Uri(path);
                await StorageFile.GetFileFromApplicationUriAsync(uri);
                return true;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            catch (Exception)
            {
                // 其他异常情况也视为不存在
                return false;
            }
        }
        public static async Task<BitmapSource> LoadWebImage(string url,bool lowRes=false)
        {
            byte[] imageBytes = await LoginService.vpn.GetByteArrayAsync(url);
            return await LoadFromBytes(imageBytes,lowRes);
        }
        public static async Task<BitmapSource> LoadLocalImage(string path)
        {
            if (path.StartsWith("ms-appx:///") || path.StartsWith("ms-appdata:///"))
            {
                // 应用资源路径
                return new BitmapImage(new Uri(await UrlEx.LocateEmoji(path)));
            }
            else if (System.IO.File.Exists(path))
            {
                // 本地文件路径
                var file = await StorageFile.GetFileFromPathAsync(path);
                using var stream = await file.OpenReadAsync();
                return await LoadFromStream(stream.AsStream());
            }
            else
            {
                // 尝试作为资源加载
                var uri = new Uri($"ms-appx:///Assets/{path}");
                return new BitmapImage(uri);
            }
        }
        public static async Task<BitmapSource> LoadFromStream(Stream stream)
        {
            using var ras = new InMemoryRandomAccessStream();
            await stream.CopyToAsync(ras.AsStream());
            ras.Seek(0);

            var bitmapImage = new BitmapImage();
            await bitmapImage.SetSourceAsync(ras);
            bitmapImage.DecodePixelHeight = 48;
            bitmapImage.DecodePixelWidth = 48;
            return bitmapImage;
        }

        public static async Task<BitmapSource> LoadFromBytes(byte[] bytes,bool lowRes=false)
        {
            using var ras = new InMemoryRandomAccessStream();
            await ras.WriteAsync(bytes.AsBuffer());
            ras.Seek(0);
            var bitmapImage = new BitmapImage();
            if (lowRes)
            {
                bitmapImage.DecodePixelHeight = 64;
                bitmapImage.DecodePixelWidth = 64;
            }
            await bitmapImage.SetSourceAsync(ras);
            
            return bitmapImage;
        }
    }
    public static class ColorEx
    {

        public static string GenerateMorandiColorHex()
        {
            var random = new Random();
            double hue = random.Next(0, 360);

            // 低饱和度（10-30%）
            double saturation = random.Next(20, 60) / 100.0;

            // 中低明度（50-70%）
            double lightness = random.Next(50, 70) / 100.0;
            // 将 HSL 转换为 RGB
            var (r, g, b) = HslToRgb(hue, saturation, lightness);


            // 转换为十六进制
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        // HSL 转 RGB 辅助函数
        private static (byte r, byte g, byte b) HslToRgb(double h, double s, double l)
        {
            double c = (1 - Math.Abs(2 * l - 1)) * s;
            double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
            double m = l - c / 2;

            (double r, double g, double b) rgb = h switch
            {
                < 60 => (c, x, 0),
                < 120 => (x, c, 0),
                < 180 => (0, c, x),
                < 240 => (0, x, c),
                < 300 => (x, 0, c),
                _ => (c, 0, x)
            };

            byte R = (byte)((rgb.r + m) * 255);
            byte G = (byte)((rgb.g + m) * 255);
            byte B = (byte)((rgb.b + m) * 255);

            return (R, G, B);
        }
    }
    

    
    
    public static class Win32Interop
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetDpiForWindow(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        public const int GWL_STYLE = -16;
        public const int WS_THICKFRAME = 0x00040000;
        public static readonly IntPtr HWND_TOP = new(0);
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOZORDER = 0x0004;
    }
}
