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
        // return y switch
        // {
        //     <= 0  => TextureAtlas.NameToVoxelId("bedrock"),
        //     <= 6  => TextureAtlas.NameToVoxelId("stone"),
        //     <= 9  => TextureAtlas.NameToVoxelId("dirt"),
        //     <= 10 => TextureAtlas.NameToVoxelId("grass_block"),
        //     _     => TextureAtlas.NameToVoxelId("air")
        // };

        if (y >= 15) return 0u;
        
        // Generate height value based on noise
        float noise = FractalNoise(x, z);
        int height = (int)(noise * 15); // Scale noise to a suitable height range

        if (y <= 0)
        {
            return TextureAtlas.NameToVoxelId("bedrock");
        }
        else if (y < height - 3)
        {
            return TextureAtlas.NameToVoxelId("stone");
        }
        else if (y < height - 1)
        {
            return TextureAtlas.NameToVoxelId("dirt");
        }
        else if (y < height)
        {
            return TextureAtlas.NameToVoxelId("grass_block");
        }
        else if (y == height)
        {
            return Random.Hash((uint)(new Vector3Int(x, y, z).GetHashCode())) > 0.99f
                ? TextureAtlas.NameToVoxelId("oak_log") : 0u;
        }
        else if (y == height + 1)
        {
            return Random.Hash((uint)(new Vector3Int(x, y - 1, z).GetHashCode())) > 0.99f
                ? TextureAtlas.NameToVoxelId("oak_log") : 0u;
        }
        else if (y == height + 2)
        {
            return Random.Hash((uint)(new Vector3Int(x, y - 2, z).GetHashCode())) > 0.99f
                ? TextureAtlas.NameToVoxelId("oak_log") : 0u;
        }
        else if (y == height + 3)
        {
            return Random.Hash((uint)(new Vector3Int(x, y - 3, z).GetHashCode())) > 0.99f
                ? TextureAtlas.NameToVoxelId("oak_log") : 0u;
        }
        else
        {
            return TextureAtlas.NameToVoxelId("air");
        }
    }
}