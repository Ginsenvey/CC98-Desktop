using System.Threading.Tasks;
using Windows.Media.Core;

namespace CC98.Controls.Primitives;

public interface IMediaLoader
{
    Task<MediaSource?> LoadMedia(string src);
}


