using VoxelGame.Maths;

namespace VoxelGame;

// Static class that holds time information
public static class Time
{
    /// Time since the last frame
    public static float DeltaTime { get; internal set; }
    
    /// Time since the first frame
    public static float ElapsedTime { get; internal set; }

    /// Number of frames since the first frame
    public static int NumFrames { get; internal set; }
    
    /// Current framerate
    public static int Fps { get; private set; }
    
    internal static void UpdateFps()
    {
        Fps = Mathf.RoundToInt(1f / DeltaTime);
    }
}