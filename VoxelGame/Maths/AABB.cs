namespace VoxelGame.Maths;

public struct AABB : IEquatable<AABB>
{
    public Vector3 Min;
    public Vector3 Max;

    public Vector3 Center => (Min + Max) * 0.5f;

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

    public void SetCenter(Vector3 pos)
    {
        Min = pos - (Max - Min) / 2;
        Max = pos + (Max - Min) / 2;
    }

    public void Encapsulate(Vector3 point)
    {
        Min = Vector3.Min(Min, point);
        Max = Vector3.Max(Max, point);
    }

    public void Encapsulate(AABB box)
    {
        Min = Vector3.Min(Min, box.Min);
        Max = Vector3.Max(Max, box.Max);
    }

    public bool Contains(Vector3 point)
    {
        return point.X >= Min.X && point.X <= Max.X && point.Y >= Min.Y && point.Y <= Max.Y && point.Z >= Min.Z &&
               point.Z <= Max.Z;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Min, Max);
    }

    public bool Equals(AABB other)
    {
        return Min.Equals(other.Min) && Max.Equals(other.Max);
    }

    public override bool Equals(object? obj)
    {
        return obj is AABB other && Equals(other);
    }
}