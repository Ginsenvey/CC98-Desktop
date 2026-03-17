using Microsoft.UI.Xaml.Media.Imaging;
using System.Threading.Tasks;
using Windows.Media.Core;

namespace CC98.Share.Controls.Primitives
{
    public interface IImageLoader
    {
        Task<BitmapSource?> LoadImage(string src);
    }
}