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
}