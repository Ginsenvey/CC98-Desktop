using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using CC98.Kernel.Authorize;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace CC98.Kernel.Network;

/// <summary>
///     协调器类，确保令牌刷新操作的线程安全和单次执行。
/// </summary>
public sealed class Coordinator
{
    /// <summary>
    ///     受保护的构造方法。
    /// </summary>
    private Coordinator()
    {
    }

    /// <summary>
    ///     对象的唯一实例。
    /// </summary>
    public static Coordinator Instance { get; } = new();

    // 使用Lazy<Task<bool>>确保线程安全的单次执行和结果共享
    private Lazy<Task<bool>>? RefreshTask { get; set; }

    /// <summary>
    ///     用于协调刷新的锁对象。
    /// </summary>
    private Lock Lock { get; } = new();

    /// <summary>
    ///     执行刷新的核心方法。
    /// </summary>
    /// <returns>表示异步操作的任务。操作结果表示刷新是否成功。</returns>
    private async Task<bool> SilentAuthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var r = await LoginService.GetRefreshTokenAsync(cancellationToken);
            if (r == "1")
            {
                return true;
            }
            else if (r.StartsWith("2"))
            {
                //统一处理令牌失效情况，通知用户并退出应用
                //触发此处未必是令牌过期，也可能是其他登录失败的情况。
                ApplicationData.Current.LocalSettings.Values["IsActive"] = 0;
                var notification = new AppNotificationBuilder()
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
            lock (Lock)
            {
                RefreshTask = null;
            }
        }
    }

    // 公开的安全调用接口
    public Task<bool> SafeSilentAuth()
    {
        lock (Lock)
        {
            // 如果当前没有进行中的刷新任务，创建新任务
            if (RefreshTask == null || RefreshTask.Value.IsCompleted) RefreshTask = new(() => SilentAuthAsync());

            return RefreshTask.Value;
        }
    }
}