using VoxelGame.Graphics.Shaders;
using VoxelGame.Maths;

namespace VoxelGame.Graphics;

public class DeferredPreprocessor : IDisposable
{
    public Shader Shader { get; set; }

    public int Texture => _fboTex;
    
    private int _vao, _vbo, _ebo;
    
    private Vector2Int size;

    private int _fbo, _fboTex;
    
    public DeferredPreprocessor(Shader shader, Vector2Int size)
    {
        Shader = shader;
        
        this.size = size;
        
        float[] data =
        [
            // Position             // Uvs
            -1.0f, -1.0f, 0.0f,     0.0f, 0.0f,
            -1.0f,  1.0f, 0.0f,     0.0f, 1.0f,
             1.0f, -1.0f, 0.0f,     1.0f, 0.0f,
             1.0f,  1.0f, 0.0f,     1.0f, 1.0f,
        ];
        int[] triangles =
        [
            0, 1, 2,
            2, 1, 3,
        ]; 
        
        _vao = GL.GenVertexArray();
        GL.BindVertexArray(_vao);
        
        _vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float), data, BufferUsageHint.StaticDraw);
        
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0 * sizeof(float));
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(1);
        
        _ebo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, triangles.Length * sizeof(int), triangles, BufferUsageHint.StaticDraw);
        
        GL.BindVertexArray(0);
        
        GenerateFramebuffer(size);
    }
    
    private void GenerateFramebuffer(Vector2Int size)
    {
        // Generate and bind framebuffer
        _fbo = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        
        // texture setup
        _fboTex = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _fboTex);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb16f, size.X, size.Y, 0, PixelFormat.Rgb,
            PixelType.Float, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D, _fboTex, 0);

        // Check framebuffer completeness
        var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != FramebufferErrorCode.FramebufferComplete)
        {
            throw new Exception($"Framebuffer is not complete: {status}");
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }
    
    public void Resize(Vector2Int newSize)
    {
        if (newSize != size)
        {
            Dispose();
            size = newSize;
            GenerateFramebuffer(size);
        }
    }

    public void Render(bool bindShader = true)
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        
        if (bindShader)
            Shader.Use();
        
        GL.BindVertexArray(_vao);
        GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0);
        
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Dispose()
    {
        GL.DeleteFramebuffer(_fbo);
        GL.DeleteTexture(_fboTex);
    }
}