namespace VoxelGame.Maths;

public static class Random
{
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

    private static System.Random _random;

    private static void InitRandom()
    {
        _random = new System.Random();
    }
}