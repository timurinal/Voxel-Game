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
    
    public static Texture2D AlbedoTexture { get; private set; }
    public static Texture2D SpecularTexture { get; private set; }

    internal static int[][] Voxels =
    [ // Format: front face id, back id, top id, bottom id, right id, left id
        [ 1, 1, 0, 2, 1, 1 ], // Grass block
        [ 2, 2, 2, 2, 2, 2 ], // Dirt block
        [ 3, 3, 3, 3, 3, 3 ], // Stone block
        [ 4, 4, 4, 4, 4, 4 ], // Bedrock block
    ];

    internal static void Init()
    {
        AlbedoTexture = new(AlbedoAtlasPath, false, false, true);
        SpecularTexture = new(SpecularAtlasPath, false, false, true);
    }
    
    internal static Vector2 GetUVForVoxelFace(int voxelID, VoxelFace face, int u, int v)
    {
        int textureID = Voxels[voxelID][(int)face];
        int texturePerRow = AtlasWidth / VoxelTextureSize;
        float unit = 1.0f / texturePerRow;

        // Padding to avoid bleeding
        const float padding = 0.0001f;

        float x = (textureID % texturePerRow) * unit + padding;
        float y = (textureID / texturePerRow) * unit + padding;
        float adjustedUnit = unit - 2 * padding;

        return new Vector2(x + u * adjustedUnit, y + v * adjustedUnit);
    }

    /// <summary>
    /// Converts a voxel name to its corresponding voxel ID.
    /// </summary>
    /// <param name="name">The name of the voxel.</param>
    /// <returns>The voxel ID.</returns>
    /// <remarks>Returns 0 if the name is not found</remarks>
    public static uint NameToVoxelId(string name)
    {
        return name switch
        {
            "grass_block" => 1,
            "dirt" => 2,
            "stone" => 3,
            "bedrock" => 4,
            _ => 0
        };
    }
}