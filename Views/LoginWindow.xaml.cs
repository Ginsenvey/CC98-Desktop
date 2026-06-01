using CC98.Kernel;
using CC98.Kernel.Authorize;
using CC98.Kernel.Network;
using CC98.Objects;
using CC98.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.Storage;
using Windows.System;
using Windows.UI.WindowManagement;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CC98.Views;

/// <summary>
///     登录窗口。
/// </summary>
public sealed partial class LoginWindow : Window
{
    //TODO：改xaml
    private const string TipText= "如果尚未连接浙江大学内网，请在此处登录WebVPN,或者使用[ZJU Connect](https://github.com/Mythologyli/ZJU-Connect-for-Windows/releases).";
    private const string GuideText= "**在校外登录** \r\n\r\n先配置应用的内建WebVPN,再使用密码登录。\r\n\r\n**忘记密码/无账号？**\r\n\r\n进入[CC98](https://www.cc98.org/logon)官网操作。\r\n\r\n**遇到问题/想要新功能?**\r\n\r\n你可以在微软商店或[开发进度记录楼](https://www.cc98.org/topic/6173309)反馈此问题。\r\n\r\n你也可以克隆本应用仓库，自由修改和编译新的分支。不过，在分发时，应当告知所有的改动。\r\n\r\n**成为开发者**\r\n\r\n本应用使用`WinUI3`,`C#`,`XAML`构建。欢迎所有对.NET生态感兴趣的uu加入本应用的开发，欢迎所有使用者对本应用UI、功能和代码提供建议。";
    
    
   
    public LoginWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(GridTitleBar);
        RootGrid.RequestedTheme = ElementTheme.Light;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;
        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "cc98.ico");
        AppWindow.SetIcon(iconPath);
        AppWindow.SetTaskbarIcon(iconPath);
        SetWindowState();

    }
    /// <summary>
    /// 适应高分屏，设置窗口大小为基于2560分辨率的缩放值，并居中显示。
    /// </summary>
    private void SetWindowState()
    {
        const int baseWidth = 1440;
        const int baseHeight = 920;

        var presenter = OverlappedPresenter.Create();
        presenter.IsResizable = false;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        presenter.SetBorderAndTitleBar(true, true);
        AppWindow.SetPresenter(presenter);

        var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        int screenWidth = displayArea.WorkArea.Width;  // 物理分辨率宽度

        // 计算 scale（基于2560基准）
        double scale = screenWidth / 2560.0;
        // 可选：限制范围，避免窗口过小或过大
        scale = Math.Clamp(scale, 0.6, 1.5);
        // 计算最终想要的内容区大小（逻辑像素）
        int desiredClientWidth = (int)(baseWidth * scale);
        int desiredClientHeight = (int)(baseHeight * scale);
        AppWindow.ResizeClient(new SizeInt32(desiredClientWidth, desiredClientHeight));

        //居中
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest)?.WorkArea;
        if (area == null) return;
        AppWindow.Move(new((area.Value.Width - AppWindow.Size.Width) / 2,(area.Value.Height - AppWindow.Size.Height) / 2));
    }

    private void RootGrid_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
LoginFrame.Navigate(typeof(ForumLoginPage));
        }
        catch(Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
        
    }
}