using OpenTK.Mathematics;
using VoxelGame.Maths;
using VoxelGame.Rendering;
using Vector3 = VoxelGame.Maths.Vector3;

public class Frustum
{
    private Player _player;
    
    private Vector3 _nearTopLeft;
    private Vector3 _nearTopRight;
    private Vector3 _nearBottomLeft;
    private Vector3 _nearBottomRight;
    private Vector3 _farTopLeft;
    private Vector3 _farTopRight;
    private Vector3 _farBottomLeft;
    private Vector3 _farBottomRight;

    private Plane[] _planes;

    public Frustum(Player player)
    {
        _player = player;
        
        RecalculateFrustum(player.Fov, player.Aspect);
    }

    internal void RecalculateFrustum(float fov, float aspect)
    {
        CalculateFrustumPoints(fov, aspect);
        CalculatePlanes();
    }

    private void CalculateFrustumPoints(float fov, float aspect)
    {
        float nearClipPlane = _player.NearClipPlane;
        float farClipPlane = _player.FarClipPlane;

        // Calculate dimensions of the near and far clip planes
        float tanHFov = MathF.Tan(fov * 0.5f);
        float nearHeight = tanHFov * nearClipPlane;
        float nearWidth = nearHeight * aspect;
        float farHeight = tanHFov * farClipPlane;
        float farWidth = farHeight * aspect;

        // Calculate points (in world space) on the near clip plane
        Vector3 nearCenter = _player.Position + _player.Forward * nearClipPlane;
        _nearTopLeft = nearCenter + (_player.Up * nearHeight - _player.Right * nearWidth);
        _nearTopRight = nearCenter + (_player.Up * nearHeight + _player.Right * nearWidth);
        _nearBottomLeft = nearCenter - (_player.Up * nearHeight - _player.Right * nearWidth);
        _nearBottomRight = nearCenter - (_player.Up * nearHeight + _player.Right * nearWidth);

        // Calculate points (in world space) on the far clip plane
        Vector3 farCenter = _player.Position + _player.Forward * farClipPlane;
        _farTopLeft = farCenter + (_player.Up * farHeight - _player.Right * farWidth);
        _farTopRight = farCenter + (_player.Up * farHeight + _player.Right * farWidth);
        _farBottomLeft = farCenter - (_player.Up * farHeight - _player.Right * farWidth);
        _farBottomRight = farCenter - (_player.Up * farHeight + _player.Right * farWidth);
    }

    private void CalculatePlanes()
    {
        _planes = new Plane[6];
        
        // Front (near) plane
        _planes[0] = PlaneFromPoints(_nearTopLeft, _nearTopRight, _nearBottomRight);
        // Back (far) plane
        _planes[1] = PlaneFromPoints(_farTopRight, _farTopLeft, _farBottomLeft);
        // Top plane
        _planes[2] = PlaneFromPoints(_nearTopLeft, _farTopLeft, _farTopRight);
        // Bottom plane
        _planes[3] = PlaneFromPoints(_nearBottomRight, _farBottomRight, _farBottomLeft);
        // Left plane
        _planes[4] = PlaneFromPoints(_nearTopLeft, _nearBottomLeft, _farBottomLeft);
        // Right plane
        _planes[5] = PlaneFromPoints(_nearBottomRight, _nearTopRight, _farTopRight);
    }

    private Plane PlaneFromPoints(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 ab = b - a;
        Vector3 ac = c - a;
        Vector3 normal = Vector3.Normalize(Vector3.Cross(ab, ac));

        Plane plane = new Plane(-normal, a);
        return plane;
    }

    public bool IsPointInFrustum(Vector3 point)
    {
        foreach (var plane in _planes)
        {
            if (plane.GetDistanceToPoint(point) < 0)
            {
                return false;
            }
        }

        return true;
    }
}