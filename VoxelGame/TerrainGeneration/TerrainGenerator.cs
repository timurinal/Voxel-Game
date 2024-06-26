using SimplexNoise;
using VoxelGame.Maths;
using Random = VoxelGame.Maths.Random;

namespace VoxelGame.TerrainGeneration;

public static class TerrainGenerator
{
    public static float Sample(int x, int z, float scale = 0.01f)
    {
        return Noise.CalcPixel2D(x, z, scale) / 255f;
    }
    public static float Sample(int x, int y, int z, float scale = 0.01f)
    {
        return Noise.CalcPixel3D(x, y, z, scale) / 255f;
    }

    public static float FractalNoise(int x, int z, int octaves = 6, float frequencyBase = 2, float persistence = 0.5f)
    {
        float total = 0; // Total height
        float maxAmplitude = 0; // Maximum amplitude
        float frequency = 1;
        float amplitude = 1;

        for (int i = 0; i < octaves; i++)
        {
            total += Sample((int)(x * frequency), (int)(z * frequency)) * amplitude;

            maxAmplitude += amplitude;

            frequency *= frequencyBase;
            amplitude *= persistence;
        }

        return total / maxAmplitude; //Normalizing the total value
    }

    public static uint SampleTerrain(int x, int y, int z)
    {
        float h = Sample(x, y, z);
        float height = h * 20;
        if (y <= 0) return VoxelData.NameToVoxelId("bedrock");
        if (y <= height - 15) return VoxelData.NameToVoxelId("stone");
        if (y <= height - 12) return VoxelData.NameToVoxelId("dirt");
        if (y <= height - 11) return VoxelData.NameToVoxelId("grass_block");
        
        return VoxelData.NameToVoxelId("air");
        
        if (y <= 0) return VoxelData.NameToVoxelId("bedrock");
        if (y >= 140) return VoxelData.NameToVoxelId("air");
        
        return y switch
        {
            <= 0   => VoxelData.NameToVoxelId("bedrock"),
            <= 128 => Sample(x, y, z, 0.05f) >= 0.65f ? VoxelData.NameToVoxelId("air") : VoxelData.NameToVoxelId("stone"),
            <= 131 => VoxelData.NameToVoxelId("dirt"),
            <= 132 => VoxelData.NameToVoxelId("grass_block"),
            _      => VoxelData.NameToVoxelId("air")
        };
    }
}