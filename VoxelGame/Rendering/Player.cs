using VoxelGame.Maths;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Vector3 = VoxelGame.Maths.Vector3;
using Plane = System.Numerics.Plane;
using Vector4 = VoxelGame.Maths.Vector4;

namespace VoxelGame.Rendering;

public sealed class Player
{
    public const int ChunkRenderDistance = 8;

    public readonly Frustum Frustum;
    
    public Vector3 Position => cameraPosition;
    public float Yaw => yaw;
    public float Pitch => pitch;

    public float Aspect => _aspect;
    public float Fov => fov;
    public float VFov => fov * Mathf.Deg2Rad;
    public float HFov => 2 * Mathf.Atan(Mathf.Tan(VFov * 0.5f) * Aspect);

    public Vector3 Forward => cameraFront.Normalized;
    public Vector3 Right => Vector3.Cross(Forward, Vector3.Up).Normalized;
    public Vector3 Up => Vector3.Cross(Right, Forward).Normalized;

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

    private float _aspect;

    private float yaw = 180, pitch;

    private float MoveSpeed;
    private float RotateSpeed;

    private float fov;

    public Player(Vector2Int screenSize, float moveSpeed = 5f, float rotateSpeed = 0.5f, float fov = 65f, float near = 0.1f, float far = 7500f)
    {
        Frustum = new(this);
        MoveSpeed = moveSpeed;
        RotateSpeed = rotateSpeed;

        NearClipPlane = near;
        FarClipPlane = far;

        this.fov = fov;

        _aspect = (float)screenSize.X / screenSize.Y;
        
        ViewMatrix = Matrix4.LookAt(cameraPosition, cameraPosition + cameraFront, cameraUp);
        ProjectionMatrix = Matrix4.CreatePerspectiveFieldOfView(VFov, _aspect, 
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
    }
    
    private Plane TransformPlane(Plane plane, Matrix4 matrix)
    {
        Vector4 planeVec = new Vector4(plane.Normal.X, plane.Normal.Y, plane.Normal.Z, plane.D);
        Vector4 transformedPlaneVec = Vector4.Transform(planeVec, matrix);
        return new Plane(transformedPlaneVec);
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
}