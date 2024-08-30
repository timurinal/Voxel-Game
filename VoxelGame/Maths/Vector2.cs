namespace VoxelGame.Maths;

/// <summary>
/// Represents a 2D vector with floating point values.
/// </summary>
public struct Vector2
{
    /// <summary>
    /// X component of the vector
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// Y component of the vector
    /// </summary>
    public float Y { get; set; }

    public Vector2(float x = 0, float y = 0)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Returns the magnitude of the vector
    /// </summary>
    public float Magnitude => (float)Math.Sqrt((X * X) + (Y * Y));

    /// <summary>
    /// Returns the normalized vector.
    /// </summary>
    public Vector2 Normalized
    {
        get
        {
            float magnitude = Magnitude;
    
            if (magnitude > 0)
                return this / magnitude;
    
            return Zero;
        }
    }

    /// <summary>
    /// Shorthand way of writing Vector2(0, 0);
    /// </summary>
    public static Vector2 Zero => new(0, 0);

    /// <summary>
    /// Shorthand way of writing Vector2(1, 1);
    /// </summary>
    public static Vector2 One => new(1, 1);

    /// <summary>
    /// Shorthand way of writing Vector2(0, 1);
    /// </summary>
    public static Vector2 Up => new(0, 1);

    /// <summary>
    /// Shorthand way of writing Vector2(0, -1);
    /// </summary>
    public static Vector2 Down => new(0, -1);

    /// <summary>
    /// Shorthand way of writing Vector2(1, 0);
    /// </summary>
    public static Vector2 Right => new(1, 0);

    /// <summary>
    /// Shorthand way of writing Vector2(-1, 0);
    /// </summary>
    public static Vector2 Left => new(-1, 0);

    /// <summary>
    /// Size of a Vector2 in bytes
    /// </summary>
    public const int Size = 8;

    #region Operators

    public static Vector2 operator +(Vector2 a, Vector2 b)
    {
        return new Vector2(a.X + b.X, a.Y + b.Y);
    }

    public static Vector2 operator -(Vector2 a, Vector2 b)
    {
        return new Vector2(a.X - b.X, a.Y - b.Y);
    }

    public static Vector2 operator +(Vector2 a, float k)
    {
        return new Vector2(a.X + k, a.Y + k);
    }

    public static Vector2 operator -(Vector2 a, float k)
    {
        return new Vector2(a.X - k, a.Y - k);
    }

    public static Vector2 operator *(Vector2 v, float k)
    {
        return new Vector2(v.X * k, v.Y * k);
    }
    
    public static Vector2 operator /(Vector2 v, float k)
    {
        return new Vector2(v.X / k, v.Y / k);
    }

    public static Vector2 operator *(Vector2 a, Vector2 b)
    {
        return new Vector2(a.X * b.X, a.Y * b.Y);
    }
    
    public static Vector2 operator /(Vector2 a, Vector2 b)
    {
        return new Vector2(a.X / b.X, a.Y / b.Y);
    }

    public static bool operator ==(Vector2 a, Vector2 b)
    {
        return a.X == b.X && a.Y == b.Y;
    }

    public static bool operator !=(Vector2 a, Vector2 b)
    {
        return !(a == b);
    }

    public static Vector2 operator -(Vector2 v)
    {
        return new Vector2(-v.X, -v.Y);
    }

    public static explicit operator Vector2Int(Vector2 v)
    {
        return new Vector2Int((int)v.X, (int)v.Y);
    }

    public static implicit operator Vector3(Vector2 v)
    {
        return new Vector3(v.X, v.Y);
    }

    public static implicit operator OpenTK.Mathematics.Vector2(Vector2 v)
    {
        return new OpenTK.Mathematics.Vector2(v.X, v.Y);
    }
    
    public static implicit operator Vector2(OpenTK.Mathematics.Vector2 v)
    {
        return new Vector2(v.X, v.Y);
    }
    
    public static implicit operator System.Numerics.Vector2(Vector2 v)
    {
        return new(v.X, v.Y);
    }
    
    #endregion

    #region Utility Methods

    /// <summary>
    /// Calculates the dot product of two vectors.
    /// </summary>
    /// <param name="a">The first vector.</param>
    /// <param name="b">The second vector.</param>
    public static float Dot(Vector2 a, Vector2 b)
    {
        return a.X * b.X + a.Y * b.Y;
    }
    
    public override string ToString()
    {
        return $"({X}, {Y})";
    }

    #endregion
}