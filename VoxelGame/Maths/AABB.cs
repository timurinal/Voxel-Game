namespace VoxelGame.Maths;

public struct AABB : IEquatable<AABB>
{
    public Vector3 Min;
    public Vector3 Max;

    public Vector3 Center => (Min + Max) / 2;

    public AABB()
    {
        this = new AABB(Vector3.Zero);
    }
    public AABB(Vector3 min, Vector3 max)
    {
        if (min.X > max.X || min.Y > max.Y || min.Z > max.Z)
        {
            throw new ArgumentException("Min should be less than or equal to Max in all dimensions.");
        }
        
        Min = min;
        Max = max;
    }
    public AABB(Vector3 center, Vector3 min, Vector3 max)
    {
        if (min.X > max.X || min.Y > max.Y || min.Z > max.Z)
        {
            throw new ArgumentException("Min should be less than or equal to Max in all dimensions.");
        }
        
        Min = center - (max - min) / 2;
        Max = center + (max - min) / 2;
    }

    public AABB(Vector3 center)
    {
        Min = center - Vector3.One / 2;
        Max = center + Vector3.One / 2;
    }

    public void Move(Vector3 dir)
    {
        Min += dir;
        Max += dir;
    }
    
    public bool Contains(Vector3 point)
    {
        return (point.X >= Min.X && point.X <= Max.X) &&
               (point.Y >= Min.Y && point.Y <= Max.Y) &&
               (point.Z >= Min.Z && point.Z <= Max.Z);
    }

    public bool Intersects(AABB other)
    {
        return (Min.X <= other.Max.X && Max.X >= other.Min.X) &&
               (Min.Y <= other.Max.Y && Max.Y >= other.Min.Y) &&
               (Min.Z <= other.Max.Z && Max.Z >= other.Min.Z);
    }

    public void Expand(Vector3 amount)
    {
        Min -= amount / 2;
        Max += amount / 2;
    }

    public bool Equals(AABB other)
    {
        return Min.Equals(other.Min) && Max.Equals(other.Max);
    }

    public override bool Equals(object? obj)
    {
        return obj is AABB other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Min, Max);
    }
}