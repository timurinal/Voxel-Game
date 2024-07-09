namespace VoxelGame;

public static class AsyncTesting
{
    public static Thread MainThread { get; private set; }
    public static Thread CurrentThread => Thread.CurrentThread;
    public static bool IsMainThread => MainThread.ManagedThreadId == CurrentThread.ManagedThreadId;

    private static MainThreadSynchronisationContext _mainThreadContext;
    
    public static void Run()
    {
        MainThread = Thread.CurrentThread;

        _mainThreadContext = new MainThreadSynchronisationContext();
        SynchronizationContext.SetSynchronizationContext(_mainThreadContext);

        // Start async function on the main thread
        SomeAsyncFunction().ConfigureAwait(false);

        _mainThreadContext.RunOnCurrentThread();
    }
    
    private static async Task SomeAsyncFunction()
    {
        // Any main-thread setup here

        Console.WriteLine($"Setting up function. Is main thread: {Engine.IsMainThread}");
        
        var data = await Task.Run(() =>
        {
            // Execute code on a background thread
            var data = SomeHeavyFunc(2);
            return data;
        });

        // Finish running on the main thread (important for opengl)
        // For some reason, this always runs on the background thread that the Task.Run code was running on
        Console.WriteLine($"Finished function in {data}ms. Is main thread: {Engine.IsMainThread}");
    }
    
    private static long SomeHeavyFunc(int sleepTime)
    {
        var timer = new System.Diagnostics.Stopwatch();
        timer.Start();
    
        // Simulate some background work
        Console.WriteLine($"Running function. Is main thread: {Engine.IsMainThread}");
        Thread.Sleep(sleepTime * 1000);
    
        timer.Stop();
        return timer.ElapsedMilliseconds;
    }
}