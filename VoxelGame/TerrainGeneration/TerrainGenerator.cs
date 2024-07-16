using SimplexNoise;
using VoxelGame.Maths;
using Random = VoxelGame.Maths.Random;

namespace VoxelGame.TerrainGeneration;

public static class TerrainGenerator
{
    const int SeaLevel = 65;
    const int MaxHeight = 35;
    
    public static float Sample(int x, float scale = 0.01f)
    {
        return Noise.CalcPixel1D(x, scale) / 255f;
    }
    public static float Sample(int x, int z, float scale = 0.01f)
    {
        return Noise.CalcPixel2D(x, z, scale) / 255f;
    }
    public static float Sample(int x, int y, int z, float scale = 0.01f)
    {
        return Noise.CalcPixel3D(x, y, z, scale) / 255f;
    }

    public static float FractalNoise(int x, int z, float scale = 0.003f, int octaves = 6, float frequencyBase = 2, float persistence = 0.5f)
    {
        float total = 0; // Total height
        float maxAmplitude = 0; // Maximum amplitude
        float frequency = 1;
        float amplitude = 1;

        for (int i = 0; i < octaves; i++)
        {
            total += Sample((int)(x * frequency), (int)(z * frequency), scale) * amplitude;

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
            <= 0 => VoxelData.NameToVoxelId("bedrock"),
            <= 25 => VoxelData.NameToVoxelId("stone"),
            <= 28 => VoxelData.NameToVoxelId("dirt"),
            <= 29 => Sample(x, z) >= 0.5f ? VoxelData.NameToVoxelId("grass_block") : VoxelData.NameToVoxelId("dirt"),
            _ => VoxelData.NameToVoxelId("air")
        };

        // if (y <= 0) return VoxelData.NameToVoxelId("bedrock");
        // if (y >= MaxHeight + SeaLevel) return VoxelData.NameToVoxelId("air");
        //
        // float biomeVal = Sample(x, z, 0.005f);
        // int biome = biomeVal >= 0.5f ? 1 : 0;
        // biome = 0;
        //
        // int height = biome == 0 ? MaxHeight : 35;
        //
        // if (biome == 0) 
        // {
        //     float heightValue = FractalNoise(x, z) * height + SeaLevel;
        //     
        //     // Determine the terrain type based on the height value and y coordinate
        //     if (y > heightValue)
        //     {
        //         return VoxelData.NameToVoxelId("air"); // Above the terrain, it's air
        //     }
        //     else if (y > heightValue - 1)
        //     {
        //         return VoxelData.NameToVoxelId("grass_block"); // Top layer, use grass
        //     }
        //     else if (y > heightValue - 5)
        //     {
        //         return VoxelData.NameToVoxelId("dirt"); // Below top layer, use dirt
        //     }
        //     else
        //     {
        //         return VoxelData.NameToVoxelId("stone"); // Deep underground, use stone
        //     }
        // }
        // else
        // {
        //     float heightValue = FractalNoise(x, z, 0.004f) * height + SeaLevel;
        //     
        //     // Determine the terrain type based on the height value and y coordinate
        //     if (y > heightValue)
        //     {
        //         return VoxelData.NameToVoxelId("air"); // Above the terrain, it's air
        //     }
        //     else
        //     {
        //         return VoxelData.NameToVoxelId("stone"); // Deep underground, use stone
        //     }
        // }
    }
}