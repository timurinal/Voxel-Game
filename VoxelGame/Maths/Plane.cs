namespace VoxelGame.Maths;

public struct Plane
{
    private Vector3 _normal;
    private float _distance;

    public Plane(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        _normal = Vector3.Cross(v2 - v1, v3 - v1).Normalized;
        _distance = Vector3.Dot(_normal, v1);
    }

    public float GetDistanceFromPoint(Vector3 point)
    {
        return Vector3.Dot(_normal, point) - _distance;
    }
}