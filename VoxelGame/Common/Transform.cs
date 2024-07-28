using OpenTK.Mathematics;
using VoxelGame.Maths;
using Vector3 = VoxelGame.Maths.Vector3;
using Vector4 = OpenTK.Mathematics.Vector4;

namespace VoxelGame;

public class Transform
{
    public Vector3 Position;
    public Vector3 Rotation;
    public Vector3 Scale;

    public Transform()
    {
        Position = Vector3.Zero;
        Rotation = Vector3.Zero;
        Scale = Vector3.One;
    }

    public Transform(Vector3 position, Vector3 rotation, Vector3 scale)
    {
        Position = position;
        Rotation = rotation;
        Scale = scale;
    }

    public void LookAt(Vector3 position)
    {
        Vector3 forward = Vector3.Normalize(position - Position);
        Rotation = new Vector3(Mathf.Atan2(forward.Y, forward.Z) * Mathf.Rad2Deg,
            Mathf.Atan2(-forward.X, Mathf.Sqrt(forward.Y * forward.Y + forward.Z * forward.Z)) * Mathf.Rad2Deg, 0);
    }

    public Vector3 TransformPoint(Vector3 point)
    {
        return (new Vector4(point) * GetModelMatrix()).Xyz;
    }
    
    public static Vector3 TransformPoint(Vector3 point, ref Matrix4 matrix)
    {
        return (new Vector4(point) * matrix).Xyz;
    }

    internal Matrix4 GetModelMatrix()
    {
        Matrix4 scale = Matrix4.CreateScale(Scale);
        
        // TODO: Replace this with a quaternion as this is a lot of matrix multiplication
        Matrix4 rotX = Matrix4.CreateRotationX(Rotation.X * Mathf.Deg2Rad);
        Matrix4 rotY = Matrix4.CreateRotationY(Rotation.Y * Mathf.Deg2Rad);
        Matrix4 rotZ = Matrix4.CreateRotationZ(Rotation.Z * Mathf.Deg2Rad);
        Matrix4 rot = rotX * rotY * rotZ;
        
        Matrix4 trans = Matrix4.CreateTranslation(Position);

        return scale * rot * trans;
    }
}