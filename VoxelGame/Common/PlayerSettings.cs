namespace VoxelGame.Common;

public static class PlayerSettings
{
    /// Render distance in chunks
    public static int RenderDistance = 12;

    public static int SqrRenderDistance => RenderDistance * RenderDistance;
}