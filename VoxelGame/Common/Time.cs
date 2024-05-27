using VoxelGame.Maths;

namespace VoxelGame;

public static class Time
{
    public static float DeltaTime { get; internal set; }
    public static float FixedTimeStep { get; set; } = 0.01f;
    public static int Fps => Mathf.RoundToInt(1 / DeltaTime);
}