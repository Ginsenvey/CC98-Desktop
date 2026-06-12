using System.Threading.Tasks;
using Windows.Media.Core;
using CC98.Kernel.Authorize;
using System;
using System.Net.Http;
using Windows.Media.Streaming.Adaptive;
using CC98.Kernel;

namespace CC98.Controls.Primitives;

public class SmartMediaLoader : IMediaLoader
{
    public async Task<MediaSource?> LoadMedia(string src)
    {
        var apiService = App.Current.GetService<ApiService>();
        return await apiService.GetSourceAsync(src);
    }
}