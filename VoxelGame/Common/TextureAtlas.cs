using VoxelGame.Maths;
using VoxelGame.Rendering;

namespace VoxelGame;

internal enum VoxelFace
{
    Front,
    Back,
    Top,
    Bottom,
    Right,
    Left
}

public static class TextureAtlas
{
    public const int AtlasWidth = 256, AtlasHeight = 256;
    public const int VoxelTextureSize = 16;
    public const string AlbedoAtlasPath = "Assets/Textures/atlas-main.png";
    public const string SpecularAtlasPath = "Assets/Textures/atlas-specular.png";

    public const int FontAtlasWidth = 128, FontAtlasHeight = 128;
    public const int CharacterSize = 8;
    public const string FontAtlasPath = "Assets/Textures/atlas-font.png";
    
    public static Texture2D AlbedoTexture { get; private set; }
    public static Texture2D SpecularTexture { get; private set; }
    
    public static Texture2D FontTexture { get; private set; }

    internal static void Init()
    {
        AlbedoTexture = new(AlbedoAtlasPath, false, false, true);
        SpecularTexture = new(SpecularAtlasPath, false, true, true);
        
        FontTexture = new(FontAtlasPath, false, false, false);
    }
    
    internal static Vector2 GetUVForVoxelFace(int voxelID, VoxelFace face, int u, int v)
    {
        int textureID = VoxelData.GetTextureFace((uint)voxelID, face);
        int texturePerRow = AtlasWidth / VoxelTextureSize;
        float unit = 1.0f / texturePerRow;

        // Padding to avoid bleeding
        const float padding = 0.0001f;

        float x = (textureID % texturePerRow) * unit + padding;
        float y = (textureID / texturePerRow) * unit + padding;
        float adjustedUnit = unit - 2 * padding;

        return new Vector2(x + u * adjustedUnit, y + v * adjustedUnit);
    }
    
    internal static Vector2 GetUVForFont(int fontId, int u, int v)
    {
        int texturePerRow = FontAtlasWidth / CharacterSize;
        float unit = 1.0f / texturePerRow;

        float x = (fontId % texturePerRow) * unit;
        float y = (fontId / texturePerRow) * unit;

        return new Vector2(x + u * unit, y + v * unit);
    }
}