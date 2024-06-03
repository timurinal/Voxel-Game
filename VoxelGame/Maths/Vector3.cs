namespace VoxelGame.Maths;

/// <summary>
/// Represents a 3D vector with floating point values.
/// </summary>
public struct Vector3 : IEquatable<Vector3>
{
    /// <summary>
    /// X component of the vector
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// Y component of the vector
    /// </summary>
    public float Y { get; set; }
    
    /// <summary>
    /// Z component of the vector
    /// </summary>
    public float Z { get; set; }
    
    public Vector3(float x = 0, float y = 0, float z = 0)
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
    public Vector3 Normalized
    {
        get
        {
            float magnitude = Magnitude;
    
            if (magnitude > 0)
                return this / magnitude;
    
            return Zero;
        }
    }

    public void Normalize()
    {
        this = Normalized;
    }

    /// <summary>
    /// Shorthand way of writing Vector3(0, 0, 0);
    /// </summary>
    public static Vector3 Zero => new(0, 0, 0);

    /// <summary>
    /// Shorthand way of writing Vector3(1, 1, 1);
    /// </summary>
    public static Vector3 One => new(1, 1, 1);

    /// <summary>
    /// Shorthand way of writing Vector3(0, 1, 0);
    /// </summary>
    public static Vector3 Up => new(0, 1, 0);

    /// <summary>
    /// Shorthand way of writing Vector3(0, -1, 0);
    /// </summary>
    public static Vector3 Down => new(0, -1, 0);

    /// <summary>
    /// Shorthand way of writing Vector3(1, 0, 0);
    /// </summary>
    public static Vector3 Right => new(1, 0, 0);

    /// <summary>
    /// Shorthand way of writing Vector3(-1, 0, 0);
    /// </summary>
    public static Vector3 Left => new(-1, 0, 0);
    
    /// <summary>
    /// Shorthand way of writing Vector3(0, 0, 1);
    /// </summary>
    public static Vector3 Forward => new(0, 0, 1);
    
    /// <summary>
    /// Shorthand way of writing Vector3(0, 0, -1);
    /// </summary>
    public static Vector3 Back => new(0, 0, -1);
    
    /// <summary>
    /// Size of a Vector3 in bytes
    /// </summary>
    public const int Size = 12;
    
    #region Operators

    public static Vector3 operator +(Vector3 a, Vector3 b)
    {
        return new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    }

    public static Vector3 operator -(Vector3 a, Vector3 b)
    {
        return new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    }

    public static Vector3 operator +(Vector3 a, float k)
    {
        return new Vector3(a.X + k, a.Y + k, a.Z + k);
    }

    public static Vector3 operator -(Vector3 a, float k)
    {
        return new Vector3(a.X - k, a.Y - k, a.Z - k);
    }

    public static Vector3 operator *(Vector3 v, float k)
    {
        return new Vector3(v.X * k, v.Y * k, v.Z * k);
    }
    public static Vector3 operator *(float k, Vector3 v)
    {
        return new Vector3(v.X * k, v.Y * k, v.Z * k);
    }
    
    public static Vector3 operator /(Vector3 v, float k)
    {
        return new Vector3(v.X / k, v.Y / k, v.Z / k);
    }

    public static Vector3 operator *(Vector3 a, Vector3 b)
    {
        return new Vector3(a.X * b.X, a.Y * b.Y, a.X * b.Z);
    }
    
    public static Vector3 operator /(Vector3 a, Vector3 b)
    {
        return new Vector3(a.X / b.X, a.Y / b.Y, a.Z / b.Z);
    }

    public static bool operator ==(Vector3 a, Vector3 b)
    {
        return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
    }

    public static bool operator !=(Vector3 a, Vector3 b)
    {
        return !(a == b);
    }

    public static Vector3 operator -(Vector3 v)
    {
        return new Vector3(-v.X, -v.Y, -v.Z);
    }

    public static implicit operator Vector3(Vector3Int v)
    {
        return new Vector3(v.X, v.Y, v.Z);
    }
    
    public static implicit operator OpenTK.Mathematics.Vector3(Vector3 v)
    {
        return new OpenTK.Mathematics.Vector3(v.X, v.Y, v.Z);
    }
    public static implicit operator System.Numerics.Vector3(Vector3 v)
    {
        return new(v.X, v.Y, v.Z);
    }
    public static implicit operator Vector3(OpenTK.Mathematics.Vector3 v)
    {
        return new Vector3(v.X, v.Y, v.Z);
    }
    public static implicit operator Vector3(System.Numerics.Vector3 v)
    {
        return new(v.X, v.Y, v.Z);
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

    public static Vector3 Normalize(Vector3 v)
    {
        return v.Normalized;
    }

    public static Vector3 Cross(Vector3 a, Vector3 b)
    {
        return new Vector3((a.Y * b.Z) - (a.Z * b.Y), (a.Z * b.X) - (a.X * b.Z),
            (a.X * b.Y) - (a.Y * b.X));
    }

    public override string ToString()
    {
        return $"({X}, {Y}, {Z})";
    }
    
    public static Vector3 Rotate(Vector3 v, Vector3 degrees)
    {
        Vector3 k = degrees.Normalized;
        float theta = degrees.Magnitude * Mathf.PI / 180; // angle in radians

        Vector3 vRot = v * Mathf.Cos(theta)
                       + Cross(k, v) * Mathf.Sin(theta)
                       + k * Dot(k, v) * (1 - Mathf.Cos(theta));
        return vRot;
    }

    public static Vector3 Radians(Vector3 v)
    {
        return new Vector3(Mathf.DegToRad(v.X), Mathf.DegToRad(v.Y), Mathf.DegToRad(v.Z));
    }

    public static Vector3Int Round(Vector3 v) =>
        new(Mathf.RoundToInt(v.X), Mathf.RoundToInt(v.Y), Mathf.RoundToInt(v.Z));
    
    public static Vector3 Round(Vector3 v, int decimals) =>
        new(Mathf.Round(v.X, decimals), Mathf.Round(v.Y, decimals), Mathf.Round(v.Z, decimals));

    public static Vector3 RotateX(Vector3 vector, float angle)
    {
        return new Vector3(vector.X, vector.Y * Mathf.Cos(angle) - vector.Z * Mathf.Sin(angle),
            vector.Y * Mathf.Sin(angle) + vector.Z * Mathf.Cos(angle));
    }
    
    public static Vector3 RotateY(Vector3 vector, float angle)
    {
        return new Vector3(vector.X * Mathf.Cos(angle) + vector.Z * Mathf.Sin(angle), vector.Y,
            -vector.X * Mathf.Sin(angle) + vector.Z * Mathf.Cos(angle));
    }

    public static float SqrDistance(Vector3 a, Vector3 b)
    {
        return (a - b).SqrMagnitude;
    }

    public static float Distance(Vector3 a, Vector3 b)
    {
        return (a - b).Magnitude;
    }
    
    #endregion

    public bool Equals(Vector3 other)
    {
        return Mathf.Abs(X - other.X) < 0.0000001f && Mathf.Abs(Y - other.Y) < 0.0000001f &&
               Mathf.Abs(Z - other.Z) < 0.0000001f;
    }

    public override bool Equals(object? obj)
    {
        return obj is Vector3 other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z);
    }
}