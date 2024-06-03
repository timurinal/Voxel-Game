using System.Runtime.InteropServices;

namespace VoxelGame;

public class SimplexNoise
{
    [DllImport("libs/libSimplexNoise.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void init_noise(int seed);

    [DllImport("libs/libSimplexNoise.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern double simplex_noise(double xin, double yin);

    public SimplexNoise(int seed)
    {
        init_noise(seed);
    }

    public double Sample(double x, double y) => simplex_noise(x, y);
    public float Sample(float x, float y) => (float)simplex_noise(x, y);
}