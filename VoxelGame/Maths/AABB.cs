﻿namespace VoxelGame.Maths;

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
}