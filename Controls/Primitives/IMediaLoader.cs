using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Core;

namespace CC98.Share.Controls.Primitives;

public interface IMediaLoader
{
    Task<MediaSource?> LoadMedia(string src);
}


