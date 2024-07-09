using System.Collections.Concurrent;

namespace VoxelGame;

public class MainThreadSynchronisationContext : SynchronizationContext
{
    private readonly ConcurrentQueue<(SendOrPostCallback, object)> _workItems = new ConcurrentQueue<(SendOrPostCallback, object)>();
    private readonly AutoResetEvent _workItemsAvailable = new AutoResetEvent(false);
    private bool _running = true;

    public override void Send(SendOrPostCallback d, object state)
    {
        d(state);
    }

    public override void Post(SendOrPostCallback d, object state)
    {
        _workItems.Enqueue((d, state));
        _workItemsAvailable.Set();
    }

    public void RunOnCurrentThread()
    {
        while (_running)
        {
            _workItemsAvailable.WaitOne();

            while (_workItems.TryDequeue(out var workItem))
            {
                workItem.Item1(workItem.Item2);
            }
        }
    }

    public void Complete()
    {
        _running = false;
        _workItemsAvailable.Set();
    }
}