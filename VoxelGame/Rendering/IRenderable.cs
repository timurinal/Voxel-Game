namespace VoxelGame.Rendering;

public interface IRenderable
{
    internal (int vertexCount, int triangleCount) Render(Player player);
}