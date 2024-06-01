using VoxelGame.Maths;
using VoxelGame.Rendering;

namespace VoxelGame;

public static class TextureAtlas
{
    public const int AtlasWidth = 256, AtlasHeight = 256;
    public const int VoxelTextureSize = 16;
    public const string AlbedoAtlasPath = "Assets/Textures/atlas-main.png";
    public const string SpecularAtlasPath = "Assets/Textures/atlas-specular.png";
    
    public static Texture2D AlbedoTexture { get; private set; }
    public static Texture2D SpecularTexture { get; private set; }

    internal static void Init()
    {
        AlbedoTexture = new(AlbedoAtlasPath, false, false, true);
        SpecularTexture = new(SpecularAtlasPath, false, false, true);
    }
}