using VoxelGame.Maths;
using VoxelGame.Rendering;

namespace VoxelGame;

public static class TextureAtlas
{
    public const int AtlasWidth = 256, AtlasHeight = 256;
    public const int VoxelTextureSize = 16;
    public const string AtlasPath = "Textures/atlas-main.png";
    
    public static Texture2D AtlasTexture { get; private set; }

    internal static void Init()
    {
        AtlasTexture = new(AtlasPath, false, false, true);
    }
}