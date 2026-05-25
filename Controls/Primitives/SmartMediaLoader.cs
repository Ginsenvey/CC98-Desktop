using System.Threading.Tasks;
using Windows.Media.Core;
using CC98.Kernel.Authorize;
using System;

namespace CC98.Controls.Primitives;

public class SmartMediaLoader : IMediaLoader
{
    public async Task<MediaSource?> LoadMedia(string src)
    {
        //return await VpnService.GetSourceAsync(src);
        throw new NotImplementedException();
    }
}