using VoxelGame.Maths;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Vector3 = VoxelGame.Maths.Vector3;

namespace VoxelGame.Rendering;

public sealed class Player
{
    public const int ChunkRenderDistance = 8;
    
    public Vector3 Position => cameraPosition;
    public float Yaw => yaw;
    public float Pitch => pitch;
    
    public AABB Collider = AABB.PlayerAABB(Vector3.Zero);
    
    public Matrix4 ProjectionMatrix;
    public Matrix4 ViewMatrix;
    public Matrix4 VPMatrix { get; private set; }
    
    public float NearClipPlane { get; set; }
    public float FarClipPlane { get; set; }
    
    public Plane[] Planes { get; private set; } = new Plane[6];

    private Vector3 cameraTarget = Vector3.Zero;
    private Vector3 cameraPosition = new(0, 10, 0);
    private Vector3 cameraDirection = Vector3.Zero;
    private Vector3 cameraUp = Vector3.Up;
    private Vector3 cameraRight = Vector3.Right;
    private Vector3 cameraFront = Vector3.Back;
    private Vector3 _cameraDirection;

    private float yaw = 180, pitch;

    private float MoveSpeed;
    private float RotateSpeed;

    private float fov;

    public Player(Vector2Int screenSize, float moveSpeed = 5f, float rotateSpeed = 0.5f, float fov = 65f, float near = 0.1f, float far = 7500f)
    {
        MoveSpeed = moveSpeed;
        RotateSpeed = rotateSpeed;

        NearClipPlane = near;
        FarClipPlane = far;

        this.fov = fov;
        
        ViewMatrix = Matrix4.LookAt(cameraPosition, cameraPosition + cameraFront, cameraUp);
        ProjectionMatrix = Matrix4.CreatePerspectiveFieldOfView(fov * Mathf.Deg2Rad, (float)screenSize.X / screenSize.Y, 
            NearClipPlane, FarClipPlane);
    }

    internal void Update(Vector2Int screenSize)
    {
        if (Input.GetKey(Keys.W)) cameraPosition += MoveSpeed * cameraFront * Time.DeltaTime;
        if (Input.GetKey(Keys.S)) cameraPosition -= MoveSpeed * cameraFront * Time.DeltaTime;
        if (Input.GetKey(Keys.A))
            cameraPosition -= Vector3.Normalize(Vector3.Cross(cameraFront, cameraUp)) * MoveSpeed * Time.DeltaTime;
        if (Input.GetKey(Keys.D))
            cameraPosition += Vector3.Normalize(Vector3.Cross(cameraFront, cameraUp)) * MoveSpeed * Time.DeltaTime;

        if (Input.GetKey(Keys.E)) cameraPosition.Y += MoveSpeed * Time.DeltaTime;
        if (Input.GetKey(Keys.Q)) cameraPosition.Y -= MoveSpeed * Time.DeltaTime;

        pitch = Mathf.Clamp(pitch, -89, 89);

        _cameraDirection.X = Mathf.Cos(yaw * Mathf.Deg2Rad) * Mathf.Cos(pitch * Mathf.Deg2Rad);
        _cameraDirection.Y = Mathf.Sin(pitch * Mathf.Deg2Rad);
        _cameraDirection.Z = Mathf.Sin(yaw * Mathf.Deg2Rad) * Mathf.Cos(pitch * Mathf.Deg2Rad);

        cameraFront = _cameraDirection.Normalized;

        cameraTarget = cameraPosition + _cameraDirection.Normalized;

        cameraDirection = (cameraPosition - cameraTarget).Normalized;
        cameraRight = Vector3.Normalize(Vector3.Cross(Vector3.Up, cameraDirection));
        cameraUp = Vector3.Cross(cameraDirection, cameraRight);

        ViewMatrix = Matrix4.LookAt(cameraPosition, cameraPosition + cameraFront, cameraUp);
        ProjectionMatrix = Matrix4.CreatePerspectiveFieldOfView(fov * Mathf.Deg2Rad, (float)screenSize.X / screenSize.Y, 
            NearClipPlane, FarClipPlane);

        VPMatrix = ViewMatrix * ProjectionMatrix;
        
        // Extract the planes from the view-projection matrix
        Planes[0] = new Plane(
            VPMatrix.M41 + VPMatrix.M11,
            VPMatrix.M42 + VPMatrix.M12,
            VPMatrix.M43 + VPMatrix.M13,
            VPMatrix.M44 + VPMatrix.M14); // Left

        Planes[1] = new Plane(
            VPMatrix.M41 - VPMatrix.M11,
            VPMatrix.M42 - VPMatrix.M12,
            VPMatrix.M43 - VPMatrix.M13,
            VPMatrix.M44 - VPMatrix.M14); // Right

        Planes[2] = new Plane(
            VPMatrix.M41 + VPMatrix.M21,
            VPMatrix.M42 + VPMatrix.M22,
            VPMatrix.M43 + VPMatrix.M23,
            VPMatrix.M44 + VPMatrix.M24); // Bottom

        Planes[3] = new Plane(
            VPMatrix.M41 - VPMatrix.M21,
            VPMatrix.M42 - VPMatrix.M22,
            VPMatrix.M43 - VPMatrix.M23,
            VPMatrix.M44 - VPMatrix.M24); // Top

        Planes[4] = new Plane(
            VPMatrix.M41 + VPMatrix.M31,
            VPMatrix.M42 + VPMatrix.M32,
            VPMatrix.M43 + VPMatrix.M33,
            VPMatrix.M44 + VPMatrix.M34); // Near

        Planes[5] = new Plane(
            VPMatrix.M41 - VPMatrix.M31,
            VPMatrix.M42 - VPMatrix.M32,
            VPMatrix.M43 - VPMatrix.M33,
            VPMatrix.M44 - VPMatrix.M34); // Far

        // Normalize the planes
        for (int i = 0; i < 6; i++)
        {
            Planes[i].Normalize();
        }
    }

    internal void UpdateProjection(Vector2Int screenSize, float fov = 65f)
    {
        this.fov = fov;
        ProjectionMatrix = Matrix4.CreatePerspectiveFieldOfView(fov * Mathf.Deg2Rad, (float)screenSize.X / screenSize.Y, 
            NearClipPlane, FarClipPlane);
    }

    public void Move(Vector3 dir)
    {
        cameraPosition += dir;
        Collider = AABB.PlayerAABB(cameraPosition);
    }

    public void Rotate(float yaw, float pitch)
    {
        this.yaw += yaw * RotateSpeed;
        this.pitch += -pitch * RotateSpeed;
    }
    
    public void SetPosition(Vector3 pos)
    {
        cameraPosition = pos;
    }

    public bool IsPointInFrustum(Vector3 point)
    {
        return Planes.All(plane => plane.DistanceToPoint(point) >= 0);
    }
    
    public bool IsBoxInFrustum(AABB box)
    {
        foreach (var plane in Planes)
        {
            Vector3 positiveVertex = new Vector3(
                plane.A >= 0 ? box.Max.X : box.Min.X,
                plane.B >= 0 ? box.Max.Y : box.Min.Y,
                plane.C >= 0 ? box.Max.Z : box.Min.Z);

            if (plane.DistanceToPoint(positiveVertex) < 0)
            {
                return false; // The box is outside this plane
            }
        }

        return true; // The box is inside or intersects the frustum
    }
}