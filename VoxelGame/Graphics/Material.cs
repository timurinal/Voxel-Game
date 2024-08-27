using VoxelGame.Graphics.Shaders;

namespace VoxelGame.Graphics;

public class Material
{
    public enum SurfaceMode
    {
        Opaque,
        Transparent
    }
    
    public Shader Shader;
    public SurfaceMode surfaceMode;

    public Texture2D MainTexture;

    public Material(Shader shader)
    {
        Shader = shader;
        MainTexture = null;
    }
    
    public Material(Shader shader, Texture2D mainTexture)
    {
        Shader = shader;
        MainTexture = mainTexture;
    }

    internal void Use(TextureUnit texUnit = TextureUnit.Texture0)
    {
        Shader.Use();
        
        if (MainTexture != null)
            MainTexture.Use(texUnit);
    }
}