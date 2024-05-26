namespace VoxelGame.Maths;

public struct Plane
{
    public float A, B, C, D;

    public Plane(float a, float b, float c, float d)
    {
        A = a;
        B = b;
        C = c;
        D = d;
    }

    public void Normalize()
    {
        float length = (float)Math.Sqrt(A * A + B * B + C * C);
        A /= length;
        B /= length;
        C /= length;
        D /= length;
    }

    public float DistanceToPoint(Vector3 point)
    {
        return A * point.X + B * point.Y + C * point.Z + D;
    }
}