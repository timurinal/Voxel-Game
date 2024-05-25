using VoxelGame.Rendering;

namespace VoxelGame;

public static class TextureAtlas
{
    public const string AtlasPath = "Textures/atlas-main.png";
    
    public static Texture2D AtlasTexture { get; private set; }

    internal static void Init()
    {
        AtlasTexture = new(AtlasPath, false, false, true);
    }
}