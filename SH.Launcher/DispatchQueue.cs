using Avalonia.Threading;
using SH.Framework.Extensions;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SH.Launcher;

/// <summary>
/// This class dispatches events from backend to frontend's UI thread in a sequenced way:
/// </summary>
public sealed class DispatchQueue : IAsyncDisposable
{
    private readonly Channel<Func<Task>> Queue;
    private readonly CancellationTokenSource CTS = new();
    private readonly Task ProcessingTask;

    public DispatchQueue()
    {
        Queue = Channel.CreateUnbounded<Func<Task>>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });
        ProcessingTask = Task.Run(LoopAsync);
    }

    public bool Run(Func<Task> item) =>
        Queue.Writer.TryWrite(item);

    public bool Run(Action action) =>
        Run(() => { action(); return Task.CompletedTask; });

    public ValueTask EnqueueAsync(Func<Task> item, CancellationToken cancellationToken = default) =>
        Queue.Writer.WriteAsync(item, cancellationToken);

    private async Task LoopAsync()
    {
        try
        {
            await foreach (Func<Task> task in Queue.Reader.ReadAllAsync(CTS.Token))
                await Dispatcher.UIThread.InvokeAsync(async () => await task(), DispatcherPriority.Normal, CTS.Token);
        }
        catch (Exception ex) when (ex.IsOperationCancelled()) { }
    }

    public async ValueTask DisposeAsync()
    {
        Queue.Writer.TryComplete();
        CTS.Cancel();
        try { await ProcessingTask; }
        catch (Exception ex) when (ex.IsOperationCancelled()) { }
        CTS.Dispose();
    }
}
