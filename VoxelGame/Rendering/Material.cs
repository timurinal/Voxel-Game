namespace VoxelGame.Rendering;

public class Material
{
    public enum SurfaceMode
    {
        Opaque,
        Transparent
    }
    
    public Shader Shader;
    public SurfaceMode surfaceMode;

    public Material(Shader shader)
    {
        Shader = shader;
    }

    internal void Use() => Shader.Use();
}