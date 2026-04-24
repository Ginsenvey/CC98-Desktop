using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Threading.Tasks;

namespace CC98.Share.Controls.Primitives;

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