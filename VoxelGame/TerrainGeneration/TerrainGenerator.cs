using SimplexNoise;

namespace VoxelGame.TerrainGeneration;

public static class TerrainGenerator
{
    public static float Sample(int x, int z)
    {
        return Noise.CalcPixel2D(x, z, 0.01f) / 255f;
    }
    public static float Sample(int x, int y, int z)
    {
        return Noise.CalcPixel3D(x, y, z, 0.10f) / 255f;
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
        else
        {
            return TextureAtlas.NameToVoxelId("air");
        }
    }
}