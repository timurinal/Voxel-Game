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
        return y switch
        {
            <= 0   => VoxelData.NameToVoxelId("bedrock"),
            <= 128 => Sample(x, y, z, 0.05f) >= 0.65f ? VoxelData.NameToVoxelId("air") : VoxelData.NameToVoxelId("stone"),
            <= 131 => VoxelData.NameToVoxelId("dirt"),
            <= 132 => VoxelData.NameToVoxelId("grass_block"),
            _      => VoxelData.NameToVoxelId("air")
        };
        
        // Generate height value based on noise
        // float noise = FractalNoise(x, z);
        // int height = (int)(noise * 64); // Scale noise to a suitable height range
        //
        // if (y <= 0)
        // {
        //     return VoxelData.NameToVoxelId("bedrock");
        // }
        // else if (y < height - 3)
        // {
        //     return Sample(x + 50, y + 50, z + 50, 0.05f) >= 0.65f ? VoxelData.NameToVoxelId("air") : VoxelData.NameToVoxelId("stone");
        // }
        // else if (y < height - 1)
        // {
        //     return VoxelData.NameToVoxelId("dirt");
        // }
        // else if (y < height)
        // {
        //     return VoxelData.NameToVoxelId("grass_block");
        // }
        // else
        // {
        //     return VoxelData.NameToVoxelId("air");
        // }
    }
}