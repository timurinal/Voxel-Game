namespace VoxelGame.Maths;

public struct AABB
{
    public Vector3 Min;
    public Vector3 Max;

    public AABB(Vector3 min, Vector3 max)
    {
        Min = min;
        Max = max;
    }

    public AABB(Vector3 centre, float halfWidth, float halfHeight, float halfDepth)
    {
        Min = centre - new Vector3(halfWidth, halfHeight, halfDepth);
        Max = centre + new Vector3(halfWidth, halfHeight, halfDepth);
    }

    public static AABB CreateVoxelAABB(Vector3 voxelPosition)
    {
        return new AABB(voxelPosition, 0.5f, 0.5f, 0.5f);
    }
    
    public static AABB PlayerAABB(Vector3 position)
    {
        // create a player aabb with a width and depth of 0.6 and a height of 1.85
        return new AABB(position - new Vector3(0.3f, 0, 0.3f), position + new Vector3(0.3f, 1.85f, 0.3f));
    }

    public bool Intersects(AABB other)
    {
        return (Min.X <= other.Max.X && Max.X >= other.Min.X) &&
               (Min.Y <= other.Max.Y && Max.Y >= other.Min.Y) &&
               (Min.Z <= other.Max.Z && Max.Z >= other.Min.Z);
    }
    
    public Vector3 GetPenetrationDepth(AABB other)
    {
        float xDepth = Math.Min(Max.X - other.Min.X, other.Max.X - Min.X);
        float yDepth = Math.Min(Max.Y - other.Min.Y, other.Max.Y - Min.Y);
        float zDepth = Math.Min(Max.Z - other.Min.Z, other.Max.Z - Min.Z);

        return new Vector3(xDepth, yDepth, zDepth);
    }

    public bool PointInAABB(Vector3 point)
    {
        return (point.X >= Min.X && point.X <= Max.X) && (point.Y >= Min.Y && point.Y <= Max.Y) &&
               (point.Z >= Min.Z && point.Z <= Max.Z);
    }
}