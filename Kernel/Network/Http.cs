using CC98.Kernel.Authorize;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Storage.Streams;

namespace CC98.Kernel.Network;
//Todo：使用Polly重写异常处理逻辑，在Kernel中直接使用HttpClient发送请求，删除VpnService中的GetAsync、PostAsync等方法，保留SendRequestAsync作为核心请求发送函数，并在Kernel中实现自动重试和令牌刷新逻辑。这样可以简化VpnService的职责，使其专注于VPN连接和令牌管理，而将请求发送和错误处理的逻辑集中在Kernel中，提升代码的清晰度和可维护性。
//那么，VpnService应当负责维护一个Client，进行Cookie注入和URL转写

