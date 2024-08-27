using StbImageSharp;
using VoxelGame;
using Environment = VoxelGame.Environment;

namespace VoxelGame.Graphics;

public class Texture2D
{
    private int _handle;

    public Texture2D(string path, bool flip = true, bool useLinearSampling = true, bool generateMipmaps = true, int anisoLevel = -1)
    {
        _handle = GL.GenTexture();
        
        Use();
        
        if (flip) StbImage.stbi_set_flip_vertically_on_load(1);
        
        ImageResult image = ImageResult.FromStream(File.OpenRead(path), ColorComponents.RedGreenBlueAlpha);

        if (useLinearSampling)
        {
            if (generateMipmaps)
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            else 
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        }
        else
        {
            if (generateMipmaps)
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.NearestMipmapLinear);
            else 
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        }
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        // -1 means anisotropic filtering is disabled
        if (anisoLevel != -1)
        {
            float maxAnisotropy = GL.GetFloat(GetPName.MaxTextureMaxAnisotropy);
            float aniso = maxAnisotropy > anisoLevel ? anisoLevel : maxAnisotropy;
            
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxAnisotropy, aniso);
        }
        
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 
            0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
        
        if (generateMipmaps)
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
    }
    
    public Texture2D(Stream stream, bool flip = true, bool useLinearSampling = true, bool generateMipmaps = true)
    {
        _handle = GL.GenTexture();
        
        Use();
        
        if (flip) StbImage.stbi_set_flip_vertically_on_load(1);
        
        ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        if (useLinearSampling)
        {
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        }
        else
        {
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        }
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 
            0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
        
        if (generateMipmaps)
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
    }
    
    public Texture2D(byte[] bytes, bool flip = true, bool useLinearSampling = true, bool generateMipmaps = true)
    {
        _handle = GL.GenTexture();
        
        Use();
        
        if (flip) StbImage.stbi_set_flip_vertically_on_load(1);
        
        ImageResult image = ImageResult.FromMemory(bytes, ColorComponents.RedGreenBlueAlpha);

        if (useLinearSampling)
        {
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        }
        else
        {
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        }
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 
            0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
        
        if (generateMipmaps)
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
    }

    public static Texture2D LoadFromAssembly(string location, bool flip = true, bool useLinearSampling = true, bool generateMipmaps = true)
    {
        if (Environment.LoadAssemblyStream(location, out var stream))
            return new Texture2D(stream, flip, useLinearSampling, generateMipmaps);
        else
        {
            Debug.LogWarning($"Failed to load image from location ({location}). Using fallback texture");
            return new Texture2D(NoTextureImage, flip, false, false);
        }
    }
    
    internal void Use(TextureUnit unit = TextureUnit.Texture0)
    {
        GL.ActiveTexture(unit);
        GL.BindTexture(TextureTarget.Texture2D, _handle);
    }
    
    internal static byte[] NoTextureImage =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x40, 0x04, 0x03, 0x00, 0x00, 0x00, 0x58, 0x47, 0x6C,
        0xED, 0x00, 0x00, 0x00, 0x01, 0x73, 0x52, 0x47, 0x42, 0x00, 0xAE, 0xCE, 0x1C, 0xE9, 0x00, 0x00,
        0x00, 0x04, 0x67, 0x41, 0x4D, 0x41, 0x00, 0x00, 0xB1, 0x8F, 0x0B, 0xFC, 0x61, 0x05, 0x00, 0x00,
        0x00, 0x06, 0x50, 0x4C, 0x54, 0x45, 0xFF, 0x00, 0xFF, 0x00, 0x00, 0x00, 0x9F, 0xA6, 0x14, 0xF2,
        0x00, 0x00, 0x00, 0x09, 0x70, 0x48, 0x59, 0x73, 0x00, 0x00, 0x0E, 0xC3, 0x00, 0x00, 0x0E, 0xC3,
        0x01, 0xC7, 0x6F, 0xA8, 0x64, 0x00, 0x00, 0x00, 0x31, 0x49, 0x44, 0x41, 0x54, 0x48, 0xC7, 0x63,
        0x00, 0x01, 0x41, 0x20, 0xC0, 0x47, 0x8F, 0x04, 0x05, 0xA3, 0x61, 0x00, 0x51, 0x30, 0x82, 0xBC,
        0x8A, 0x57, 0xC1, 0x68, 0x18, 0x8C, 0xA6, 0x07, 0xC1, 0xD1, 0xF4, 0x80, 0xA1, 0x60, 0x04, 0x79,
        0x15, 0xAF, 0x82, 0xD1, 0x30, 0x00, 0xD2, 0x0C, 0x0C, 0x00, 0x98, 0xA8, 0x44, 0x01, 0x00, 0x4C,
        0x6A, 0xFB, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82
    ];
}