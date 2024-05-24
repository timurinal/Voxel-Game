namespace VoxelGame.Maths;

public sealed class Bounds
{
    public Vector3 Min;
    public Vector3 Max;

    public Vector3 Centre
    {
        get
        {
            return (Min + Max) / 2;
        }
        set
        {
            Vector3 oldCentre = Centre;
            Vector3 translation = value - oldCentre;
            Min += translation;
            Max += translation;
        }
    }

    public Bounds(Vector3 centre)
    {
        Centre = centre;
    }

    public void Encapsulate(Vector3 point)
    {
        Min = new Vector3(Math.Min(point.X, Min.X), Math.Min(point.Y, Min.Y), Math.Min(point.Z, Min.Z));
        Max = new Vector3(Math.Max(point.X, Max.X), Math.Max(point.Y, Max.Y), Math.Max(point.Z, Max.Z));
    }

    public void Encapsulate(Bounds bounds)
    {
        Min = new Vector3(Math.Min(bounds.Min.X, Min.X), Math.Min(bounds.Min.Y, Min.Y), Math.Min(bounds.Min.Z, Min.Z));
        Max = new Vector3(Math.Max(bounds.Max.X, Max.X), Math.Max(bounds.Max.Y, Max.Y), Math.Max(bounds.Max.Z, Max.Z));
    }

    public bool Contains(Vector3 point)
    {
        return point.X >= Min.X && point.X <= Max.X && point.Y >= Min.Y && point.Y <= Max.Y && point.Z >= Min.Z &&
               point.Z <= Max.Z;
    }

    public bool Contains(Bounds bounds)
    {
        return bounds.Min.X >= Min.X && bounds.Max.X <= Max.X && bounds.Min.Y >= Min.Y && bounds.Max.Y <= Max.Y &&
               bounds.Min.Z >= Min.Z && bounds.Max.Z <= Max.Z;
    }
}