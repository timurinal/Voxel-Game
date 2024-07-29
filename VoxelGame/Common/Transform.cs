using OpenTK.Mathematics;
using Maths_Vector3 = VoxelGame.Maths.Vector3;
using Vector3 = VoxelGame.Maths.Vector3;

namespace VoxelGame;

using Vector3 = Maths_Vector3;

public struct Transform
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

    internal Matrix4 GenerateModelMatrix()
    {
        // Create scale matrix
        Matrix4 m_scale = Matrix4.CreateScale(Scale);
        
        // Create rotation matrix around each axis
        Matrix4 m_rot = Matrix4.CreateRotationX(Rotation.X) * Matrix4.CreateRotationY(Rotation.Y) * Matrix4.CreateRotationZ(Rotation.Z);
        
        // Create translation matrix
        Matrix4 m_pos = Matrix4.CreateTranslation(Position);

        // Multiply in this order to ensure correct transformation
        return m_scale * m_rot * m_pos;
    }
}