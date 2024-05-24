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
    
    private static System.Random _random;

    private static void InitRandom()
    {
        _random = new System.Random();
    }
}