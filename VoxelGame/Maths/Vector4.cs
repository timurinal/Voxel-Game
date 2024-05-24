using OpenTK.Mathematics;

namespace VoxelGame.Maths;

public struct Vector4
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
    
    /// <summary>
    /// W component of the vector
    /// </summary>
    public float W { get; set; }
    
    public Vector4(float x = 0, float y = 0, float z = 0, float w = 1)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }


    /// <summary>
    /// Gets the magnitude (length) of the vector.
    /// </summary>
    /// <returns>The magnitude of the vector.</returns>
    public float Magnitude => (float)Math.Sqrt(X * X + Y * Y + Z * Z + W * W);

    /// <summary>
    /// Returns a normalized vector. The magnitude of the resulting vector will be 1.
    /// </summary>
    /// <returns>The normalized vector.</returns>
    public Vector4 Normalized
    {
        get
        {
            var magnitude = Magnitude;
            return new Vector4(X / magnitude, Y / magnitude, Z / magnitude, W / magnitude);
        }
    }
    
    /// <summary>
    /// Shorthand way of writing Vector4(0, 0, 0, 1);
    /// </summary>
    public static Vector4 Zero => new(0, 0, 0, 1);

    /// <summary>
    /// Shorthand way of writing Vector4(1, 1, 1, 1);
    /// </summary>
    public static Vector4 One => new(1, 1, 1, 1);

    /// <summary>
    /// Shorthand way of writing Vector4(0, 1, 0, 1);
    /// </summary>
    public static Vector4 Up => new(0, 1, 0, 1);

    /// <summary>
    /// Shorthand way of writing Vector4(0, -1, 0, 1);
    /// </summary>
    public static Vector4 Down => new(0, -1, 0, 1);

    /// <summary>
    /// Shorthand way of writing Vector4(1, 0, 0, 1);
    /// </summary>
    public static Vector4 Right => new(1, 0, 0, 1);

    /// <summary>
    /// Shorthand way of writing Vector4(-1, 0, 0, 1);
    /// </summary>
    public static Vector4 Left => new(-1, 0, 0, 1);
    
    /// <summary>
    /// Shorthand way of writing Vector4(0, 0, 1, 1);
    /// </summary>
    public static Vector4 Forward => new(0, 0, 1, 1);
    
    /// <summary>
    /// Shorthand way of writing Vector4(0, 0, -1, 1);
    /// </summary>
    public static Vector4 Back => new(0, 0, -1, 1);
    
    /// <summary>
    /// Size of a Vector4 in bytes
    /// </summary>
    public const int Size = 16;
    
    #region Operators

    public static Vector4 operator +(Vector4 a, Vector4 b)
    {
        return new Vector4(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);
    }

    public static Vector4 operator -(Vector4 a, Vector4 b)
    {
        return new Vector4(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);
    }

    public static Vector4 operator +(Vector4 a, float k)
    {
        return new Vector4(a.X + k, a.Y + k, a.Z + k, a.W + k);
    }

    public static Vector4 operator -(Vector4 a, float k)
    {
        return new Vector4(a.X - k, a.Y - k, a.Z - k, a.W + k);
    }

    public static Vector4 operator *(Vector4 v, float k)
    {
        return new Vector4(v.X * k, v.Y * k, v.Z * k, v.W * k);
    }
    
    public static Vector4 operator /(Vector4 v, float k)
    {
        return new Vector4(v.X / k, v.Y / k, v.Z / k, v.W / k);
    }

    public static Vector4 operator *(Vector4 a, Vector4 b)
    {
        return new Vector4(a.X * b.X, a.Y * b.Y, a.X * b.Z, a.W * b.W);
    }
    
    public static Vector4 operator /(Vector4 a, Vector4 b)
    {
        return new Vector4(a.X / b.X, a.Y / b.Y, a.Z / b.Z, a.W / b.W);
    }

    public static bool operator ==(Vector4 a, Vector4 b)
    {
        return a.X == b.X && a.Y == b.Y && a.Z == b.Z && a.W == b.W;
    }

    public static bool operator !=(Vector4 a, Vector4 b)
    {
        return !(a == b);
    }
    
    public static Vector4 operator -(Vector4 v)
    {
        return new Vector4(-v.X, -v.Y, -v.Z, -v.W);
    }

    // TODO: Create my own Matrix struct so that all maths types are custom implemented.
    public static Vector4 operator *(Vector4 v, Matrix4 matrix4)
    {
        return new Vector4(v.X * matrix4.M11 + v.Y * matrix4.M21 + v.Z * matrix4.M31 + v.W * matrix4.M41,
            v.X * matrix4.M12 + v.Y * matrix4.M22 + v.Z * matrix4.M32 + v.W * matrix4.M42,
            v.X * matrix4.M13 + v.Y * matrix4.M23 + v.Z * matrix4.M33 + v.W * matrix4.M43,
            v.X * matrix4.M14 + v.Y * matrix4.M24 + v.Z * matrix4.M34 + v.W * matrix4.M44);
    }

    public static explicit operator Vector3(Vector4 v)
    {
        return new Vector3(v.X, v.Y, v.Z);
    }

    public static implicit operator Vector4(Vector3 v)
    {
        return new Vector4(v.X, v.Y, v.Z);
    }
    
    public static implicit operator OpenTK.Mathematics.Vector4(Vector4 v)
    {
        return new OpenTK.Mathematics.Vector4(v.X, v.Y, v.Z, v.W);
    }
    
    #endregion
    
    // TODO: Add more functions like Cross, Dot, Lerp, etc
    
    public static float Dot(Vector4 a, Vector4 b)
    {
        return a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;
    }
}