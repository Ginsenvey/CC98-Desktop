using System;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;

namespace CC98.Services;

public static class DispatcherQueueExtensions
{
    public static async Task EnqueueAsync(this DispatcherQueue dispatcher,
        Action action,
        DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
    {
        var tcs = new TaskCompletionSource<bool>();

        if (!dispatcher.TryEnqueue(priority, () =>
            {
                try
                {
                    action();
                    tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }))
        {
            tcs.TrySetException(new InvalidOperationException("Failed to enqueue the action"));
        }

        await tcs.Task;
    }
}