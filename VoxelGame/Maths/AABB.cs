namespace VoxelGame.Maths;

public struct AABB : IEquatable<AABB>
{
    public Vector3 Min, Max;

    public Vector3 Center => (Min + Max) * 0.5f;

    private Vector3[] corners;

    public AABB(Vector3 min, Vector3 max)
    {
        Min = min;
        Max = max;
        
        corners =
        [
            new Vector3(Min.X, Min.Y, Min.Z),
            new Vector3(Min.X, Min.Y, Max.Z),
            new Vector3(Min.X, Max.Y, Min.Z),
            new Vector3(Min.X, Max.Y, Max.Z),
            new Vector3(Max.X, Min.Y, Min.Z),
            new Vector3(Max.X, Min.Y, Max.Z),
            new Vector3(Max.X, Max.Y, Min.Z),
            new Vector3(Max.X, Max.Y, Max.Z)
        ];
    }

    public AABB(Vector3 offset)
    {
        Min = new Vector3(-0.5f, -0.5f, -0.5f) + offset;
        Max = new Vector3(0.5f, 0.5f, 0.5f) + offset;
        
        corners =
        [
            new Vector3(Min.X, Min.Y, Min.Z),
            new Vector3(Min.X, Min.Y, Max.Z),
            new Vector3(Min.X, Max.Y, Min.Z),
            new Vector3(Min.X, Max.Y, Max.Z),
            new Vector3(Max.X, Min.Y, Min.Z),
            new Vector3(Max.X, Min.Y, Max.Z),
            new Vector3(Max.X, Max.Y, Min.Z),
            new Vector3(Max.X, Max.Y, Max.Z)
        ];
    }

    public static AABB CreateFromExtents(Vector3 center, Vector3 extents)
    {
        return new AABB(center - extents, center + extents);
    }

    public void Move(Vector3 dir)
    {
        Min += dir;
        Max += dir;
        
        corners =
        [
            new Vector3(Min.X, Min.Y, Min.Z),
            new Vector3(Min.X, Min.Y, Max.Z),
            new Vector3(Min.X, Max.Y, Min.Z),
            new Vector3(Min.X, Max.Y, Max.Z),
            new Vector3(Max.X, Min.Y, Min.Z),
            new Vector3(Max.X, Min.Y, Max.Z),
            new Vector3(Max.X, Max.Y, Min.Z),
            new Vector3(Max.X, Max.Y, Max.Z)
        ];
    }

    public void SetCenter(Vector3 pos)
    {
        Min = pos - (Max - Min) / 2;
        Max = pos + (Max - Min) / 2;
        
        corners =
        [
            new Vector3(Min.X, Min.Y, Min.Z),
            new Vector3(Min.X, Min.Y, Max.Z),
            new Vector3(Min.X, Max.Y, Min.Z),
            new Vector3(Min.X, Max.Y, Max.Z),
            new Vector3(Max.X, Min.Y, Min.Z),
            new Vector3(Max.X, Min.Y, Max.Z),
            new Vector3(Max.X, Max.Y, Min.Z),
            new Vector3(Max.X, Max.Y, Max.Z)
        ];
    }

    public void Encapsulate(Vector3 point)
    {
        Min = Vector3.Min(Min, point);
        Max = Vector3.Max(Max, point);
        
        corners =
        [
            new Vector3(Min.X, Min.Y, Min.Z),
            new Vector3(Min.X, Min.Y, Max.Z),
            new Vector3(Min.X, Max.Y, Min.Z),
            new Vector3(Min.X, Max.Y, Max.Z),
            new Vector3(Max.X, Min.Y, Min.Z),
            new Vector3(Max.X, Min.Y, Max.Z),
            new Vector3(Max.X, Max.Y, Min.Z),
            new Vector3(Max.X, Max.Y, Max.Z)
        ];
    }

    public void Encapsulate(AABB box)
    {
        Min = Vector3.Min(Min, box.Min);
        Max = Vector3.Max(Max, box.Max);
        
        corners =
        [
            new Vector3(Min.X, Min.Y, Min.Z),
            new Vector3(Min.X, Min.Y, Max.Z),
            new Vector3(Min.X, Max.Y, Min.Z),
            new Vector3(Min.X, Max.Y, Max.Z),
            new Vector3(Max.X, Min.Y, Min.Z),
            new Vector3(Max.X, Min.Y, Max.Z),
            new Vector3(Max.X, Max.Y, Min.Z),
            new Vector3(Max.X, Max.Y, Max.Z)
        ];
    }

    public bool Contains(Vector3 point)
    {
        return point.X >= Min.X && point.X <= Max.X && point.Y >= Min.Y && point.Y <= Max.Y && point.Z >= Min.Z &&
               point.Z <= Max.Z;
    }

    public static bool Intersects(AABB a, AABB b)
    {
        return !(a.Max.X < b.Min.X || a.Min.X > b.Max.X || a.Max.Y < b.Min.Y || a.Min.Y > b.Max.Y ||
                 a.Max.Z < b.Min.Z || a.Min.Z > b.Max.Z);
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

    public Vector3[] GetCorners()
    {
        return corners;
    }
}