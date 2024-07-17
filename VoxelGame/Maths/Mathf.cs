namespace VoxelGame.Maths;

/// <summary>
/// Static class containing mathematical functions and constants.
/// </summary>
public static class Mathf
{
    /// <summary>
    /// Conversion factor to convert degrees to radians.
    /// </summary>
    public const float Deg2Rad = 0.0174533f;

    /// <summary>
    /// Conversion factor to convert radians to degrees.
    /// </summary>
    public const float Rad2Deg = 57.2958f;

    /// <summary>
    /// The mathematical constant pi, which represents the ratio of a circle's circumference to its diameter.
    /// </summary>
    public const float PI = 3.1415926536f;

    public const float E = 2.7182818285f;

    public const float Epsilon = 1e-05f;

    /// <summary>
    /// Clamps a value between a minimum and maximum value.
    /// </summary>
    /// <param name="v">The value to clamp.</param>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <returns>The clamped value.</returns>
    public static float Clamp(float v, float min, float max)
    {
        if (v < min) return min;
        if (v > max) return max;

        return v;
    }

    /// <summary>
    /// Linearly interpolates between two float values.
    /// </summary>
    /// <param name="a">The start value.</param>
    /// <param name="b">The end value.</param>
    /// <param name="t">The interpolation factor. Value should be between 0 and 1.</param>
    /// <returns>The interpolated value between <paramref name="a"/> and <paramref name="b"/>.</returns>
    public static float Lerp(float a, float b, float t)
    {
        t = Clamp(0, 1, t); // Ensure that t is in the range 0, 1
        return a + (b - a) * t;
    }

    /// <summary>
    /// Returns the sign of a specified number.
    /// </summary>
    /// <param name="v">A number to evaluate.</param>
    /// <returns>
    /// -1 if the argument is less than zero,
    /// 0 if the argument is equal to zero,
    /// 1 if the argument is greater than zero.
    /// </returns>
    public static int Sign(float v)
    {
        if (v < 0) return -1;
        if (v > 0) return 1;
        return 0;
    }

    /// <summary>
    /// Rounds a float value to the nearest integer.
    /// </summary>
    /// <param name="v">The float value to round.</param>
    /// <returns>The rounded integer value.</returns>
    public static int RoundToInt(float v) => (int)Math.Round(v);

    public static float Round(float v, int decimals) => (float)Math.Round(v, decimals);

    /// <summary>
    /// Calculates the sine of a specified angle.
    /// </summary>
    /// <param name="x">The angle, in radians.</param>
    /// <returns>The sine of the angle <paramref name="x"/>.</returns>
    public static float Sin(float x) => (float)Math.Sin(x);

    /// <summary>
    /// Returns the cosine of the specified angle in radians.
    /// </summary>
    /// <param name="x">The angle, in radians.</param>
    /// <returns>The cosine of the angle.</returns>
    public static float Cos(float x) => (float)Math.Cos(x);

    /// <summary>
    /// Calculates the square root of a given number.
    /// </summary>
    /// <param name="v">The number to calculate the square root of.</param>
    /// <returns>The square root of the given number.</returns>
    public static float Sqrt(float v) => (float)Math.Sqrt(v);

    public static float Tan(float v) => (float)Math.Tan(v);
    public static float Asin(float v) => (float)Math.Asin(v);
    public static float Acos(float v) => (float)Math.Acos(v);
    public static float Atan(float v) => (float)Math.Atan(v);
    public static float Atan2(float x, float y) => (float)Math.Atan2(x, y);

    public static float DegToRad(float deg) => deg * Deg2Rad;
    public static float RadToDeg(float rad) => rad * Rad2Deg;

    public static float Pow(float v, float p) => (float)Math.Pow(v, p);

    public static float Exp(float v) => Pow(E, v);

    public static float Abs(float v)
    {
        return v >= 0 ? v : -v;
    }

    public static float MoveTowards(float v, float target, float maxDelta)
    {
        if (Mathf.Abs(target - v) <= maxDelta)
        {
            return target;
        }

        return v + Mathf.Sign(target - v) * maxDelta;
    }

    public static float Max(params float[] values)
    {
        return values.Max();
    }
    public static int Max(params int[] values)
    {
        return values.Max();
    }
    
    public static float Min(params float[] values)
    {
        return values.Min();
    }
    public static int Min(params int[] values)
    {
        return values.Min();
    }

    public static float InverseLerp(float a, float b, float value)
    {
        return (value - a) / (b - a);
    }

    public static int FloorToInt(float v) => (int)Math.Floor(v);

    public static int Floor(float v) => (int)Math.Floor(v);

    public static float Frac(float v)
    {
        return v - Mathf.Floor(v);
    }
}