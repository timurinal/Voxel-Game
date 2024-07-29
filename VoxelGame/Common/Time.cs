namespace VoxelGame;

// Static class that holds time information
public static class Time
{
    /// Time since the last frame
    public static float DeltaTime { get; internal set; }
    
    /// Time since the first frame
    public static float ElapsedTime { get; internal set; }
}