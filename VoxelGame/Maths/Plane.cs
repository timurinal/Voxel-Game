namespace VoxelGame.Maths;

public struct Plane
{
    public Vector3 normal;
    public float distance;

    public Plane(Vector3 normal, Vector3 point)
    {
        this.normal = Vector3.Normalize(normal);
        this.distance = -Vector3.Dot(this.normal, point);
    }

    public float GetDistanceToPoint(Vector3 point)
    {
        return Vector3.Dot(normal, point) + distance;
    }
}