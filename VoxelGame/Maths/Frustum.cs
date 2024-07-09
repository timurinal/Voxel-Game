using VoxelGame.Rendering;

namespace VoxelGame.Maths;


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
        _planes = new Plane[6];
        CalculateFrustum();
    }

    public void CalculateFrustum()
    {
        float tanVFov = Mathf.Tan(_player.VFov * 0.5f);
        float tanHFov = Mathf.Tan(_player.HFov * 0.5f);

        float nh = _player.NearClipPlane * tanVFov;
        float nw = _player.NearClipPlane * tanHFov;
        float fh = _player.FarClipPlane * tanVFov;
        float fw = _player.FarClipPlane * tanHFov;

        Vector3 nearCenter = _player.Position + _player.Forward * _player.NearClipPlane;
        Vector3 farCenter = _player.Position + _player.Forward * _player.FarClipPlane;

        // Near plane
        _nearTopLeft = nearCenter + (_player.Up * nh) - (_player.Right * nw);
        _nearTopRight = nearCenter + (_player.Up * nh) + (_player.Right * nw);
        _nearBottomLeft = nearCenter - (_player.Up * nh) - (_player.Right * nw);
        _nearBottomRight = nearCenter - (_player.Up * nh) + (_player.Right * nw);

        // Far plane
        _farTopLeft = farCenter + (_player.Up * fh) - (_player.Right * fw);
        _farTopRight = farCenter + (_player.Up * fh) + (_player.Right * fw);
        _farBottomLeft = farCenter - (_player.Up * fh) - (_player.Right * fw);
        _farBottomRight = farCenter - (_player.Up * fh) + (_player.Right * fw);

        // Construct planes
        _planes[0] = new Plane(_nearTopRight, _nearTopLeft, _nearBottomLeft); // Near plane
        _planes[1] = new Plane(_farTopLeft, _farTopRight, _farBottomRight);  // Far plane
        _planes[2] = new Plane(_nearTopLeft, _nearTopRight, _farTopRight);   // Top plane
        _planes[3] = new Plane(_nearBottomRight, _nearBottomLeft, _farBottomLeft); // Bottom plane
        _planes[4] = new Plane(_nearTopLeft, _nearBottomLeft, _farBottomLeft); // Left plane
        _planes[5] = new Plane(_nearBottomRight, _nearTopRight, _farBottomRight); // Right plane
    }

    public bool IsPointInFrustum(Vector3 point)
    {
        foreach (var plane in _planes)
        {
            if (plane.GetDistanceFromPoint(point) < 0)
            {
                return false; // Point is outside this plane
            }
        }
        return true; // Point is inside all planes
    }
}