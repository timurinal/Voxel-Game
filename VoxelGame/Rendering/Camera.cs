using VoxelGame.Maths;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Vector3 = VoxelGame.Maths.Vector3;

namespace VoxelGame.Rendering;

public class Camera
{
    public Vector3 Position => cameraPosition;
    public float Yaw => yaw;
    public float Pitch => pitch;
    
    internal Matrix4 ProjectionMatrix;
    internal Matrix4 ViewMatrix;

    private Vector3 cameraTarget = Vector3.Zero;
    private Vector3 cameraPosition = Vector3.Zero;
    private Vector3 cameraDirection = Vector3.Zero;
    private Vector3 cameraUp = Vector3.Up;
    private Vector3 cameraRight = Vector3.Right;
    private Vector3 cameraFront = Vector3.Back;
    private Vector3 _cameraDirection;

    private float yaw = -90f, pitch;

    private float MoveSpeed;
    private float RotateSpeed;

    private float fov;

    public Camera(Vector2Int screenSize, float moveSpeed = 5f, float rotateSpeed = 90f, float fov = 65f)
    {
        MoveSpeed = moveSpeed;
        RotateSpeed = rotateSpeed;

        this.fov = fov;
        
        ViewMatrix = Matrix4.LookAt(cameraPosition, cameraPosition + cameraFront, cameraUp);
        ProjectionMatrix = Matrix4.CreatePerspectiveFieldOfView(fov * Mathf.Deg2Rad, (float)screenSize.X / screenSize.Y, 
            0.1f, 100f);
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

        if (Input.GetKey(Keys.Up)) pitch += RotateSpeed * Time.DeltaTime;
        if (Input.GetKey(Keys.Down)) pitch -= RotateSpeed * Time.DeltaTime;
        if (Input.GetKey(Keys.Right)) yaw += RotateSpeed * Time.DeltaTime;
        if (Input.GetKey(Keys.Left)) yaw -= RotateSpeed * Time.DeltaTime;

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
            0.1f, 100f);
    }

    internal void UpdateProjection(Vector2Int screenSize, float fov = 65f)
    {
        this.fov = fov;
        ProjectionMatrix = Matrix4.CreatePerspectiveFieldOfView(fov * Mathf.Deg2Rad, (float)screenSize.X / screenSize.Y, 
            0.1f, 100f);
    }

    public void Move(Vector3 dir)
    {
        cameraPosition += dir;
    }

    public void SetPosition(Vector3 pos)
    {
        cameraPosition = pos;
    }
}