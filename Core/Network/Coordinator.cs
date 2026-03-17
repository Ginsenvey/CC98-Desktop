using Microsoft.UI.Xaml;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace CC98.Kernel.Network;

/// <summary>
/// 协调器类，确保令牌刷新操作的线程安全和单次执行。
/// </summary>
public class Coordinator
{
    // 使用Lazy<Task<bool>>确保线程安全的单次执行和结果共享
    private static Lazy<Task<bool>> _refreshTask;

    // 用于协调刷新的锁对象
    private static readonly object _lock = new object();

    // 刷新函数
    private static async Task<bool> SilentAuth()
    {
        try
        {
            var r = await LoginService.RefreshToken();
            if (r == "1")
            {
                return true;
            }
            else if (r.StartsWith("2"))
            {
                //统一处理令牌失效情况，通知用户并退出应用
                //触发此处未必是令牌过期，也可能是其他登录失败的情况。
                ApplicationData.Current.LocalSettings.Values["IsActive"] = 0;
                AppNotification notification = new AppNotificationBuilder()
                .AddText("登录过期")
                .AddText("请重新登录。")
                .BuildNotification();
                AppNotificationManager.Default.Show(notification);
                Application.Current.Exit();
                return false;
            }
            else
            {
                return false;
            }
        }
        finally
        {
            // 重置刷新状态，允许下次刷新
            lock (_lock)
            {
                _refreshTask = null;
            }
        }
    }

    // 公开的安全调用接口
    public static Task<bool> SafeSlientAuth()
    {
        lock (_lock)
        {
            // 如果当前没有进行中的刷新任务，创建新任务
            if (_refreshTask == null || _refreshTask.Value.IsCompleted)
            {
                _refreshTask = new Lazy<Task<bool>>(() => SilentAuth());
            }

            return _refreshTask.Value;
        }
    }
}
