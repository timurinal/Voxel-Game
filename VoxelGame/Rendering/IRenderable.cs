using OpenTK.Mathematics;

namespace VoxelGame.Rendering;

internal interface IRenderable
{
    internal void Render(Camera camera);
    internal void Render(Matrix4 m_projview);
    internal void Render(Matrix4 m_proj, Matrix4 m_view);
}