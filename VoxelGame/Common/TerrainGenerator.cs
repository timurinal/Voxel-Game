using SimplexNoise;

namespace VoxelGame;

public static class TerrainGenerator
{
    public static float Sample(int x, int z)
    {
        return Noise.CalcPixel2D(x, z, 0.10f) / 255f;
    }
    public static float Sample(int x, int y, int z)
    {
        return Noise.CalcPixel3D(x, y, z, 0.10f) / 255f;
    }

    public static uint SampleTerrain(int x, int y, int z)
    {
        return y switch
        {
            <= 0 => TextureAtlas.NameToVoxelId("bedrock"),
            <= 4 => TextureAtlas.NameToVoxelId("stone"),
            <= 6 => TextureAtlas.NameToVoxelId("dirt"),
            <= 7 => Sample(x, z) > 0.5f
                ? TextureAtlas.NameToVoxelId("grass_block")
                : TextureAtlas.NameToVoxelId("dirt"),
            <= 8 => Sample(x, z) > 0.5f ? 0u : TextureAtlas.NameToVoxelId("grass_block"),
            _ => 0u
        };
    }
}