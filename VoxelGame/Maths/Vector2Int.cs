namespace VoxelGame.Maths;

/// <summary>
/// Represents a 2D vector with integer values.
/// </summary>
public struct Vector2Int
{
    /// <summary>
    /// X component of the vector
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// Y component of the vector
    /// </summary>
    public int Y { get; set; }

    public Vector2Int(int x = 0, int y = 0)
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
    public Vector2 Normalized()
    {
        float magnitude = (float)Math.Sqrt((X * X) + (Y * Y));
        if(magnitude > 0)
            return new Vector2(X / magnitude, Y / magnitude);
        else
            return Vector2.Zero;
    }

    /// <summary>
    /// Shorthand way of writing Vector2Int(0, 0);
    /// </summary>
    public static Vector2Int Zero => new(0, 0);

    /// <summary>
    /// Shorthand way of writing Vector2Int(1, 1);
    /// </summary>
    public static Vector2Int One => new(1, 1);

    /// <summary>
    /// Shorthand way of writing Vector2Int(0, 1);
    /// </summary>
    public static Vector2Int Up => new(0, 1);

    /// <summary>
    /// Shorthand way of writing Vector2Int(0, -1);
    /// </summary>
    public static Vector2Int Down => new(0, -1);

    /// <summary>
    /// Shorthand way of writing Vector2Int(1, 0);
    /// </summary>
    public static Vector2Int Right => new(1, 0);

    /// <summary>
    /// Shorthand way of writing Vector2Int(-1, 0);
    /// </summary>
    public static Vector2Int Left => new(-1, 0);
    
    /// <summary>
    /// Size of a Vector2Int in bytes
    /// </summary>
    public const int Size = 8;
    
    #region Operators

    public static Vector2Int operator +(Vector2Int a, Vector2Int b)
    {
        return new Vector2Int(a.X + b.X, a.Y + b.Y);
    }

    public static Vector2Int operator -(Vector2Int a, Vector2Int b)
    {
        return new Vector2Int(a.X - b.X, a.Y - b.Y);
    }

    public static Vector2Int operator +(Vector2Int a, int k)
    {
        return new Vector2Int(a.X + k, a.Y + k);
    }

    public static Vector2Int operator -(Vector2Int a, int k)
    {
        return new Vector2Int(a.X - k, a.Y - k);
    }

    public static Vector2Int operator *(Vector2Int v, int k)
    {
        return new Vector2Int(v.X * k, v.Y * k);
    }
    
    public static Vector2 operator /(Vector2Int v, int k)
    {
        return new Vector2(v.X / (float)k, v.Y / (float)k);
    }

    public static Vector2Int operator *(Vector2Int a, Vector2Int b)
    {
        return new Vector2Int(a.X * b.X, a.Y * b.Y);
    }
    
    public static Vector2 operator /(Vector2Int a, Vector2Int b)
    {
        return new Vector2(a.X / (float)b.X, a.Y / (float)b.Y);
    }

    public static bool operator ==(Vector2Int a, Vector2Int b)
    {
        return a.X == b.X && a.Y == b.Y;
    }

    public static bool operator !=(Vector2Int a, Vector2Int b)
    {
        return !(a == b);
    }
    
    public static Vector2Int operator -(Vector2Int v)
    {
        return new Vector2Int(-v.X, -v.Y);
    }

    public static explicit operator Vector2Int(Vector2 v)
    {
        return new Vector2Int((int)v.X, (int)v.Y);
    }
    
    public static implicit operator Vector3Int(Vector2Int v)
    {
        return new Vector3Int(v.X, v.Y);
    }
    
    public static implicit operator OpenTK.Mathematics.Vector2i(Vector2Int v)
    {
        return new OpenTK.Mathematics.Vector2i(v.X, v.Y);
    }
    public static implicit operator Vector2Int(OpenTK.Mathematics.Vector2i v)
    {
        return new Vector2Int(v.X, v.Y);
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

    #endregion
}