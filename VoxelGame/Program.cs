using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using VoxelGame.Maths;

namespace VoxelGame;

class Program
{
    static void Main(string[] args)
    {
        // foreach (var voxel in VoxelData.Voxels)
        // {
        //     Console.WriteLine(voxel.ToString());
        // }
        // return;
        
        AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(GlobalExceptionHandler);
        AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
        
        GameWindowSettings gws = new()
        {
            UpdateFrequency = 240
        };

        NativeWindowSettings nws = new()
        {
            ClientSize = new(1280, 720), 
            MinimumClientSize = new(640, 360),
            Title = "Voxel Game",
            
#if DEBUG
            Flags = ContextFlags.Debug
#endif
        };

        using var engine = new Engine(gws, nws);
        engine.Run();
    }
    
    static void GlobalExceptionHandler(object sender, UnhandledExceptionEventArgs args)
    {
        Exception e = (Exception)args.ExceptionObject;

        // Write the details to a log file or your desired location here...
        System.IO.File.WriteAllText(@"C:\crash-log.txt", e.Message + "\n" + e.StackTrace);
    }

    static void OnProcessExit(object sender, EventArgs args)
    {
        // Code to execute on process exit. 
        // Useful for logging final application state.
        // For example, you could log variables that show the state of the OpenGL context,
        // which might help diagnose a GL error.
    }
}