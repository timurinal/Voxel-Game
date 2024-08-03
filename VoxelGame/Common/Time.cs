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

    /// Average framerate, decaying over time exponentially<br/>
    /// Most of the time this value is behind the actual framerate,
    /// but it gives more readable values as it doesn't change as frequently
    public static int AvgFps
    {
        get
        {
            if (_avgFps <= 0 || NumFrames <= 0)
                return 0;
            return Mathf.RoundToInt(_avgFps);
        }
    }
    
    /// The rate at which the average FPS decays over time.
    /// Smaller value will make average FPS depend more on recent FPS values
    /// Larger value will make average FPS include history more
    public static float FpsDecay { get; set; } = 0.005f;
    
    private static float _avgFps = 0;
    
    internal static void UpdateFps()
    {
        Fps = Mathf.RoundToInt(1f / DeltaTime);
        _avgFps = (FpsDecay * Fps) + ((1 - FpsDecay) * _avgFps);
    }
}