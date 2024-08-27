namespace VoxelGame.Common;

public static class PlayerSettings
{
    /// Render distance in chunks
    public static int RenderDistance = 8;

    public static int SqrRenderDistance => RenderDistance * RenderDistance;
    
    public static int PointLightShadowDrawDistance = 1;
}