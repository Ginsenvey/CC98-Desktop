using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CC98.Controls.Extensions;

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
            tcs.TrySetException(new InvalidOperationException("Failed to enqueue the action"));

        await tcs.Task;
    }
}