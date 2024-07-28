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
        // if (y <= 0) return VoxelData.NameToVoxelId("bedrock");
        // if (y >= 140) return VoxelData.NameToVoxelId("air");
        //
        // return y switch
        // {
        //     <= 0   => VoxelData.NameToVoxelId("bedrock"),
        //     <= 5 => VoxelData.NameToVoxelId("stone"),
        //     <= 8 => VoxelData.NameToVoxelId("dirt"),
        //     <= 9 => Sample(x, z) >= 0.5f ? VoxelData.NameToVoxelId("grass_block") : Sample(x, z, scale: 0.05f) >= 0.5f ? VoxelData.NameToVoxelId("glass") : VoxelData.NameToVoxelId("red_glass"),
        //     _      => VoxelData.NameToVoxelId("air")
        // };
        
        // Constants for sea level and sand depth
        const int seaLevel = 80;
        const int sandDepth = 82;

        if (y <= 0) return VoxelData.NameToVoxelId("bedrock");
        if (y >= MaxHeight + SeaLevel) return VoxelData.NameToVoxelId("air");

        int height = MaxHeight;

        float heightValue = FractalNoise(x, z) * height + SeaLevel;

        // Determine the terrain type based on the height value and y coordinate
        if (y > heightValue)
        {
            // Above the terrain, fill in water if below sea level, otherwise it's air
            return y < seaLevel ? VoxelData.NameToVoxelId("water") : VoxelData.NameToVoxelId("air");
        }
        else if (y > heightValue - 1)
        {
            // Top layer, use grass, or sand if below the sand depth
            return y < sandDepth ? VoxelData.NameToVoxelId("sand") : VoxelData.NameToVoxelId("grass_block");
        }
        else if (y > heightValue - 5)
        {
            // Below top layer, use stone if below the sand depth, otherwise use dirt
            return y < sandDepth ? VoxelData.NameToVoxelId("stone") : VoxelData.NameToVoxelId("dirt");
        }
        else
        {
            return VoxelData.NameToVoxelId("stone"); // Deep underground, use stone
        }
    }
}