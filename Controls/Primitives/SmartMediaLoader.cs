using CC98.Kernel;
using System.Threading.Tasks;
using Windows.Media.Core;

namespace CC98.Share.Controls.Primitives;

public class SmartMediaLoader : IMediaLoader
{
    public async Task<MediaSource?> LoadMedia(string src)
    {
        return await LoginService.Vpn.GetSourceAsync(src);
    }
}