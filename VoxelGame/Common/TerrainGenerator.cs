﻿using SimplexNoise;
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
}