namespace VoxelGame.Maths;

/// <summary>
/// Represents a 3D vector with integer values.
/// </summary>
public struct Vector3Int : IEquatable<Vector3Int>
{
    /// <summary>
    /// X component of the vector
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// Y component of the vector
    /// </summary>
    public int Y { get; set; }
    
    /// <summary>
    /// Z component of the vector
    /// </summary>
    public int Z { get; set; }
    
    public Vector3Int(int x = 0, int y = 0, int z = 0)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>
    /// Returns the magnitude of the vector
    /// </summary>
    public float Magnitude => (float)Math.Sqrt((X * X) + (Y * Y) + (Z * Z));
    public float SqrMagnitude => (X * X) + (Y * Y) + (Z * Z);

    /// <summary>
    /// Returns the normalized vector.
    /// </summary>
    public Vector3 Normalized()
    {
        float magnitude = (float)Math.Sqrt((X * X) + (Y * Y));
        if(magnitude > 0)
            return new Vector3(X / magnitude, Y / magnitude);
        else
            return Vector3.Zero;
    }

    /// <summary>
    /// Shorthand way of writing Vector3(0, 0, 0);
    /// </summary>
    public static Vector3Int Zero => new(0, 0, 0);

    /// <summary>
    /// Shorthand way of writing Vector3(1, 1, 1);
    /// </summary>
    public static Vector3Int One => new(1, 1, 1);

    /// <summary>
    /// Shorthand way of writing Vector3(0, 1, 0);
    /// </summary>
    public static Vector3Int Up => new(0, 1, 0);

    /// <summary>
    /// Shorthand way of writing Vector3(0, -1, 0);
    /// </summary>
    public static Vector3Int Down => new(0, -1, 0);

    /// <summary>
    /// Shorthand way of writing Vector3(1, 0, 0);
    /// </summary>
    public static Vector3Int Right => new(1, 0, 0);

    /// <summary>
    /// Shorthand way of writing Vector3(-1, 0, 0);
    /// </summary>
    public static Vector3Int Left => new(-1, 0, 0);
    
    /// <summary>
    /// Shorthand way of writing Vector3(0, 0, 1);
    /// </summary>
    public static Vector3Int Forward => new(0, 0, 1);
    
    /// <summary>
    /// Shorthand way of writing Vector3(0, 0, -1);
    /// </summary>
    public static Vector3Int Back => new(0, 0, -1);
    
    /// <summary>
    /// Size of a Vector3Int in bytes
    /// </summary>
    public const int Size = 12;

    public override string ToString()
    {
        return $"({X}, {Y}, {Z})";
    }

    public override bool Equals(object? obj)
    {
        return obj is Vector3Int other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z);
    }

    #region Operators

    public static Vector3Int operator +(Vector3Int a, Vector3Int b)
    {
        return new Vector3Int(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    }

    public static Vector3Int operator -(Vector3Int a, Vector3Int b)
    {
        return new Vector3Int(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    }

    public static Vector3Int operator +(Vector3Int a, int k)
    {
        return new Vector3Int(a.X + k, a.Y + k, a.Z + k);
    }
    
    public static Vector3 operator +(Vector3Int a, float k)
    {
        return new Vector3(a.X + k, a.Y +k, a.Z + k);
    }

    public static Vector3Int operator -(Vector3Int a, int k)
    {
        return new Vector3Int(a.X - k, a.Y - k, a.Z - k);
    }

    public static Vector3Int operator *(Vector3Int v, int k)
    {
        return new Vector3Int(v.X * k, v.Y * k, v.Z * k);
    }
    
    public static Vector3 operator /(Vector3Int v, int k)
    {
        return new Vector3(v.X / (float)k, v.Y / (float)k, v.Z / (float)k);
    }

    public static Vector3Int operator *(Vector3Int a, Vector3Int b)
    {
        return new Vector3Int(a.X * b.X, a.Y * b.Y, a.X * b.Z);
    }
    
    public static Vector3 operator /(Vector3Int a, Vector3Int b)
    {
        return new Vector3(a.X / (float)b.X, a.Y / (float)b.Y, a.Z / (float)b.Z);
    }

    public static Vector3Int operator %(Vector3Int a, int k)
    {
        return new Vector3Int(a.X % k, a.Y % k, a.Z % k);
    }

    public static bool operator ==(Vector3Int a, Vector3Int b)
    {
        return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
    }

    public static bool operator !=(Vector3Int a, Vector3Int b)
    {
        return !(a == b);
    }
    
    public static Vector3Int operator -(Vector3Int v)
    {
        return new Vector3Int(-v.X, -v.Y, -v.Z);
    }

    public static explicit operator Vector3Int(Vector3 v)
    {
        return new Vector3Int(Mathf.RoundToInt(v.X), Mathf.RoundToInt(v.Y), Mathf.RoundToInt(v.Z));
    }
    
    public static implicit operator OpenTK.Mathematics.Vector3i(Vector3Int v)
    {
        return new OpenTK.Mathematics.Vector3i(v.X, v.Y, v.Z);
    }
    public static implicit operator OpenTK.Mathematics.Vector3(Vector3Int v)
    {
        return new OpenTK.Mathematics.Vector3(v.X, v.Y, v.Z);
    }
    
    #endregion

    #region Utility Methods

    /// <summary>
    /// Calculates the dot product of two vectors.
    /// </summary>
    /// <param name="a">The first vector.</param>
    /// <param name="b">The second vector.</param>
    public static float Dot(Vector3 a, Vector3 b)
    {
        return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    }

    #endregion

    public bool Equals(Vector3Int other)
    {
        return X == other.X && Y == other.Y && Z == other.Z;
    }
}