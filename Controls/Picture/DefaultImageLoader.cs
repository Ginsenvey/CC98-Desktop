using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;

namespace CC98.Controls.Picture;

public class DefaultImageLoader : IImageLoader
{
    public async Task<BitmapSource?> LoadImage(string src)
    {
        try
        {
            if (Uri.TryCreate(src, UriKind.Absolute, out var uri))
            {
                return await Task.FromResult(new BitmapImage(uri));
            }
            return null;
        }
        catch
        {
            return null;
        }
    }
}