using VoxelGame.Maths;

namespace VoxelGame;

public static class Time
{
    public static float DeltaTime { get; internal set; }
    public static int Fps => Mathf.RoundToInt(1 / DeltaTime);
}