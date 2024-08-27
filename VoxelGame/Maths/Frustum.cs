using System.Numerics;
using OpenTK.Mathematics;
using VoxelGame.Graphics;

namespace VoxelGame.Maths;


public class Frustum
{
    private Plane[] _planes;

    public Frustum()
    {
        _planes = new Plane[6];
    }
    
    public Frustum(Matrix4 vp)
    {
        _planes = new Plane[6];
        CalculateFrustum(vp);
    }

    public void CalculateFrustum(Matrix4 vp)
    {
        Plane left   = new(new Vector3(vp[0, 3] + vp[0, 0], vp[1, 3] + vp[1, 0], vp[2, 3] + vp[2, 0]).Normalized, (vp[3, 3] + vp[3, 0]) / 
                             new Vector3(vp[0, 3] + vp[0, 0], vp[1, 3] + vp[1, 0], vp[2, 3] + vp[2, 0]).Magnitude);
        Plane right  = new(new Vector3(vp[0, 3] - vp[0, 0], vp[1, 3] - vp[1, 0], vp[2, 3] - vp[2, 0]).Normalized, (vp[3, 3] - vp[3, 0]) / 
                             new Vector3(vp[0, 3] - vp[0, 0], vp[1, 3] - vp[1, 0], vp[2, 3] - vp[2, 0]).Magnitude);

        Plane bottom = new(new Vector3(vp[0, 3] + vp[0, 1], vp[1, 3] + vp[1, 1], vp[2, 3] + vp[2, 1]).Normalized, (vp[3, 3] + vp[3, 1]) / 
                             new Vector3(vp[0, 3] + vp[0, 1], vp[1, 3] + vp[1, 1], vp[2, 3] + vp[2, 1]).Magnitude);
        Plane top    = new(new Vector3(vp[0, 3] - vp[0, 1], vp[1, 3] - vp[1, 1], vp[2, 3] - vp[2, 1]).Normalized, (vp[3, 3] - vp[3, 1]) / 
                             new Vector3(vp[0, 3] - vp[0, 1], vp[1, 3] - vp[1, 1], vp[2, 3] - vp[2, 1]).Magnitude);

        Plane near   = new(new Vector3(vp[0, 3] + vp[0, 2], vp[1, 3] + vp[1, 2], vp[2, 3] + vp[2, 2]).Normalized, (vp[3, 3] + vp[3, 2]) / 
                             new Vector3(vp[0, 3] + vp[0, 2], vp[1, 3] + vp[1, 2], vp[2, 3] + vp[2, 2]).Magnitude);
        Plane far    = new(new Vector3(vp[0, 3] - vp[0, 2], vp[1, 3] - vp[1, 2], vp[2, 3] - vp[2, 2]).Normalized, (vp[3, 3] - vp[3, 2]) / 
                             new Vector3(vp[0, 3] - vp[0, 2], vp[1, 3] - vp[1, 2], vp[2, 3] - vp[2, 2]).Magnitude);

        _planes = [ left, right, bottom, top, near, far ];
    }

    public bool IsPointInFrustum(Vector3 point)
    {
        for (int i = 0; i < _planes.Length; i++)
        {
            // The distance from the point to the plane should be calculated as follows
            float distance = Vector3.Dot(_planes[i].Normal, point) + _planes[i].D;

            // If the distance is negative, it means the point is behind the plane. 
            if (distance < 0)
            {
                return false;
            }
        }

        // If the point is not behind any plane, it's inside the frustum
        return true;
    }
    
    public bool IsBoundingBoxInFrustum(AABB box)
    {
        Vector3[] corners = box.GetCorners();

        for (int i = 0; i < _planes.Length; i++)
        {
            bool allOutside = true;

            // Check if all corners are outside this plane
            for (int j = 0; j < corners.Length; j++)
            {
                float distance = Vector3.Dot(_planes[i].Normal, corners[j]) + _planes[i].D;
                if (distance >= 0)
                {
                    allOutside = false;
                    break;
                }
            }

            // If all corners are outside this plane, the bounding box is outside the frustum
            if (allOutside)
            {
                return false;
            }
        }

        // If the bounding box is not outside any plane, it is inside the frustum
        return true;
    }
}