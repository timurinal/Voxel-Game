using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using VoxelGame;
using VoxelGame.Maths;
using Vector3 = VoxelGame.Maths.Vector3;
using Vector2 = VoxelGame.Maths.Vector2;

namespace VoxelGame.Graphics;

public class Camera
{
    private const float SlowSpeed = 0.5f;
    private const float Speed = 2f;
    private const float SprintSpeed = 5f;

    private const float Sensitivity = 0.5f;

    public Vector3 Position
    {
        get => _position;
        set => _position = value;
    }
    
    public float Fov { get; set; }
    public float NearPlane { get; set; }
    public float FarPlane { get; set; }

    public Frustum Frustum;
    
    public Matrix4 ProjectionMatrix;
    public Matrix4 ViewMatrix;
    public Matrix4 VPMatrix;
    
    private Vector3 _position;
    private Vector3 _front;
    private Vector3 _up;

    private float _yaw, _pitch;
    
    public Camera(float fov = 65f, float nearPlane = 0.05f, float farPlane = 1000f)
    {
        Fov = fov;
        NearPlane = nearPlane;
        FarPlane = farPlane;

        _position = new Vector3(0, 32, 0);
        _front = new Vector3(0, 0, -1);
        _up = new Vector3(0, 1, 0);

        Frustum = new Frustum();
    }

    public void Update()
    {
        float speed = Input.GetKey(Keys.LeftShift) ? SprintSpeed : Input.GetKey(Keys.LeftControl) ? SlowSpeed : Speed;
        
        // Update position based on input
        if (Input.GetKey(Keys.W)) _position += _front * speed * Time.DeltaTime;                         // Forward
        if (Input.GetKey(Keys.S)) _position -= _front * speed * Time.DeltaTime;                         // Backwards
        if (Input.GetKey(Keys.A)) _position -= Vector3.Cross(_front, _up) * speed * Time.DeltaTime; // Left
        if (Input.GetKey(Keys.D)) _position += Vector3.Cross(_front, _up) * speed * Time.DeltaTime; // Right

        if (Input.GetKey(Keys.E)) _position += Vector3.Up * speed * Time.DeltaTime;                     // Up
        if (Input.GetKey(Keys.Q)) _position -= Vector3.Up * speed * Time.DeltaTime;                     // Down
        
        // Generate the vectors for the camera
        Vector3 cameraTarget = Vector3.Zero;
        // Slightly misleading name, as this vector points AWAY from the camera
        Vector3 cameraDirection = (_position - cameraTarget).Normalized;

        Vector3 cameraRight = Vector3.Cross(Vector3.Up, cameraDirection).Normalized;

        Vector3 cameraUp = Vector3.Cross(cameraDirection, cameraRight);

        _front.X = Mathf.Cos(_pitch * Mathf.Deg2Rad) * Mathf.Cos(_yaw * Mathf.Deg2Rad);
        _front.Y = Mathf.Sin(_pitch * Mathf.Deg2Rad);
        _front.Z = Mathf.Cos(_pitch * Mathf.Deg2Rad) * Mathf.Sin(_yaw * Mathf.Deg2Rad);
        _front.Normalize();
        
        ViewMatrix = Matrix4.LookAt(_position, _position + _front, _up);

        VPMatrix = ViewMatrix * ProjectionMatrix;
        
        Frustum = new Frustum(VPMatrix);
    }

    public void UpdateProjection(Vector2Int size)
    {
        // Update projection with new aspect
        ProjectionMatrix = Matrix4.CreatePerspectiveFieldOfView(Fov * Mathf.Deg2Rad, (float)size.X / size.Y, NearPlane, FarPlane);
    }

    public void Rotate(Vector2 mouseDelta)
    {
        _yaw += mouseDelta.X * Sensitivity;
        _pitch -= mouseDelta.Y * Sensitivity;
        _pitch = Mathf.Clamp(_pitch, -89f, 89f);
    }
}