using System.Threading.Tasks;
using Windows.Media.Core;
using CC98.Kernel;

namespace CC98.Controls.Primitives;

public class SmartMediaLoader : IMediaLoader
{
    public async Task<MediaSource?> LoadMedia(string src)
    {
        return await LoginService.Vpn.GetSourceAsync(src);
    }
}