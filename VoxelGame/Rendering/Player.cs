using VoxelGame.Maths;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Vector3 = VoxelGame.Maths.Vector3;
using Plane = System.Numerics.Plane;
using Vector4 = VoxelGame.Maths.Vector4;

namespace VoxelGame.Rendering;

public sealed class Player
{
    public const float Bounciness = 0.01f;
    public const float Gravity = -9.81f;

    public static Vector3 ColliderSize => new Vector3(0.8f, 1.8f, 0.8f);
    public static Vector3 ColliderOffset => new Vector3(0f, -0.7f, 0f);
    
    private const float MaxRayDistance = 5f;
    
    public static int ChunkRenderDistance = 8;
    public static int SqrChunkRenderDistance => ChunkRenderDistance * ChunkRenderDistance;

    public readonly Frustum Frustum;

    public AABB Collider;
    
    public Vector3 Position => cameraPosition;
    public float Yaw
    {
        get => yaw;
        set => yaw = value;
    }
    public float Pitch
    {
        get => pitch;
        set => pitch = value;
    }

    public float Aspect => _aspect;
    public float Fov => fov;
    public float VFov => fov * Mathf.Deg2Rad;
    public float HFov => 2 * Mathf.Atan(Mathf.Tan(VFov * 0.5f) * Aspect);

    public Vector3 Forward => -cameraDirection.Normalized;
    public Vector3 Right => Vector3.Cross(Forward, Vector3.Up).Normalized;
    public Vector3 Up => Vector3.Cross(Right, Forward).Normalized;
    
    public Matrix4 ProjectionMatrix;
    public Matrix4 ViewMatrix;
    public Matrix4 VPMatrix { get; private set; }
    
    public float NearClipPlane { get; set; }
    public float FarClipPlane { get; set; }
    
    public Plane[] Planes { get; private set; } = new Plane[6];

    private Vector3 cameraTarget = Vector3.Zero;
    private Vector3 cameraPosition = new(0, 50, -0);
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

    private Vector3 _velocity;

    public Player(Vector2Int screenSize, float moveSpeed = 5f, float rotateSpeed = 0.3f, float fov = 65f, float near = 0.1f, float far = 7500f)
    {
        Collider = AABB.CreateFromExtents(Position + ColliderOffset, ColliderSize * 0.5f);
        
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

    internal void Update(Vector2Int screenSize, IEnumerable<AABB> chunkCollisions)
    {
        float speed = MoveSpeed;
        if (Input.GetKey(Keys.LeftShift)) speed *= 7;

        // _velocity += Vector3.Up * (Gravity * Time.DeltaTime);
        
        Vector3 cameraFrontXZ = new Vector3(cameraFront.X, 0, cameraFront.Z).Normalized;

        if (Input.GetKey(Keys.W)) cameraPosition += speed * cameraFront * Time.DeltaTime;
        if (Input.GetKey(Keys.S)) cameraPosition -= speed * cameraFront * Time.DeltaTime;
        
        if (Input.GetKey(Keys.A))
            cameraPosition -= Vector3.Normalize(Vector3.Cross(cameraFront, cameraUp)) * speed * Time.DeltaTime;
        if (Input.GetKey(Keys.D))
            cameraPosition += Vector3.Normalize(Vector3.Cross(cameraFront, cameraUp)) * speed * Time.DeltaTime;

        if (Input.GetKey(Keys.E)) cameraPosition.Y += speed * Time.DeltaTime;
        if (Input.GetKey(Keys.Q)) cameraPosition.Y -= speed * Time.DeltaTime;

        Vector3 predictedPosition = Position + (_velocity * Time.DeltaTime);
        AABB predictedCollider = AABB.CreateFromExtents(predictedPosition + ColliderOffset, ColliderSize * 0.5f);

        // foreach (var collision in chunkCollisions)
        // {
        //     if (AABB.Intersects(collision, predictedCollider))
        //     {
        //         _velocity = -_velocity * Bounciness;
        //     }
        // }

        if (_velocity.SqrMagnitude <= Mathf.Epsilon) _velocity = Vector3.Zero;

        cameraPosition += _velocity * Time.DeltaTime;
        
        Collider = AABB.CreateFromExtents(Position + ColliderOffset, ColliderSize * 0.5f);

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
        
        Frustum.CalculateFrustum();
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
    
    public (bool, Vector3Int, Vector3) Traverse()
    {
        // Start point
        float x1 = Position.X;
        float y1 = Position.Y;
        float z1 = Position.Z;

        // End point
        Vector3 end = Position + Forward * MaxRayDistance;
        float x2 = end.X;
        float y2 = end.Y;
        float z2 = end.Z;

        Vector3Int currentVoxelPos = new Vector3Int(Mathf.RoundToInt(x1), Mathf.RoundToInt(y1), Mathf.RoundToInt(z1));
        int stepDir = -1;

        float dx = Mathf.Sign(x2 - x1);
        float deltaX = dx != 0 ? Mathf.Abs(1 / (x2 - x1)) : float.MaxValue;
        float maxX = dx > 0 ? deltaX * (1.0f - Mathf.Frac(x1)) : deltaX * Mathf.Frac(x1);

        float dy = Mathf.Sign(y2 - y1);
        float deltaY = dy != 0 ? Mathf.Abs(1 / (y2 - y1)) : float.MaxValue;
        float maxY = dy > 0 ? deltaY * (1.0f - Mathf.Frac(y1)) : deltaY * Mathf.Frac(y1);

        float dz = Mathf.Sign(z2 - z1);
        float deltaZ = dz != 0 ? Mathf.Abs(1 / (z2 - z1)) : float.MaxValue;
        float maxZ = dz > 0 ? deltaZ * (1.0f - Mathf.Frac(z1)) : deltaZ * Mathf.Frac(z1);

        Vector3 voxelNormal = Vector3.Zero;

        while (maxX <= 1f || maxY <= 1f || maxZ <= 1f)
        {
            var result = Engine.GetVoxelAtPosition(currentVoxelPos);
            var id = result.voxelId ?? 0;
            if (id != 0)
            {
                if (stepDir == 0)
                    voxelNormal.X = -dx;
                else if (stepDir == 1)
                    voxelNormal.Y = -dy;
                else if (stepDir == 2)
                    voxelNormal.Z = -dz;

                return (true, currentVoxelPos, voxelNormal);
            }

            if (maxX < maxY)
            {
                if (maxX < maxZ)
                {
                    currentVoxelPos.X += (int)dx;
                    maxX += deltaX;
                    stepDir = 0;
                }
                else
                {
                    currentVoxelPos.Z += (int)dz;
                    maxZ += deltaZ;
                    stepDir = 2;
                }
            }
            else
            {
                if (maxY < maxZ)
                {
                    currentVoxelPos.Y += (int)dy;
                    maxY += deltaY;
                    stepDir = 1;
                }
                else
                {
                    currentVoxelPos.Z += (int)dz;
                    maxZ += deltaZ;
                    stepDir = 2;
                }
            }
        }

        return (false, Vector3Int.Zero, Vector3.Zero);
    }
}