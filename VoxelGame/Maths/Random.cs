namespace VoxelGame.Maths;

public static class Random
{
    public static int Seed
    {
        get => _seed;
        set
        {
            _seed = value;
            _random = value == int.MaxValue ? new System.Random() : new System.Random(_seed);
        }
    }

    private static int _seed;

    public static float Value
    {
        get
        {
            if (_random == null) InitRandom();

            return _random.Next(0, 100) / 100f;
        }
    }

    public static int Range(int min, int max)
    {
        if (_random == null) InitRandom();
        
        return _random.Next(min, max + 1);
    }

    public static float Hash(uint state)
    {
        state = state * 747796405 + 2891336453;
        uint result = ((state >> ((int)(state >> 28) + 4)) ^ state) * 277803737;
        return result / 4294967265.0f;
    }

    private static System.Random _random;

    private static void InitRandom()
    {
        _random = new System.Random();
    }
}