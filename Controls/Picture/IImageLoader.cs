using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;

namespace CC98.Controls.Picture;

public interface IImageLoader
{
    Task<BitmapSource?> LoadImage(string src);
}