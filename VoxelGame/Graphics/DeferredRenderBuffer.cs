using OpenTK.Graphics.OpenGL4;
using VoxelGame.Graphics.Shaders;
using VoxelGame.Maths;

internal class DeferredRenderBuffer : IDisposable
{
    public const float FramebufferScale = 1.5f;
    
    private int gBuffer;
    private int gPosition, gNormal, gAlbedo, gMaterial, gSpecular, gDepth;
    private Vector2Int size;

    private int _vao, _vbo, _ebo;
    private Shader _shader;

    public DeferredRenderBuffer(Shader shader, Vector2Int size)
    {
        this.size = size;
        GenerateFramebuffer(size);

        _shader = shader;
        _shader.Use();
        _shader.SetInt("gPosition"  , 0, autoUse: false);
        _shader.SetInt("gNormal"    , 1, autoUse: false);
        _shader.SetInt("gAlbedo"    , 2, autoUse: false);
        _shader.SetInt("gMaterial"  , 3, autoUse: false);
        _shader.SetInt("gSpecular"  , 4, autoUse: false);
        _shader.SetInt("gDepth"     , 5, autoUse: false);

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
    }

    private void GenerateFramebuffer(Vector2Int size)
    {
        size = new Vector2Int(Mathf.RoundToInt(size.X * FramebufferScale), Mathf.RoundToInt(size.Y * FramebufferScale));
        
        // Generate and bind framebuffer
        gBuffer = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, gBuffer);

        const bool LinearSampling = false;
        TextureMinFilter minFilter = LinearSampling ? TextureMinFilter.Linear : TextureMinFilter.Nearest;
        TextureMagFilter magFilter = LinearSampling ? TextureMagFilter.Linear : TextureMagFilter.Nearest;
        
        // Position texture setup
        gPosition = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, gPosition);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb16f, size.X, size.Y, 0, PixelFormat.Rgb,
            PixelType.Float, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minFilter);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)magFilter);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D, gPosition, 0);
        
        // Normal texture setup
        gNormal = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, gNormal);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb16f, size.X, size.Y, 0, PixelFormat.Rgb,
            PixelType.Float, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minFilter);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)magFilter);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1,
            TextureTarget.Texture2D, gNormal, 0);

        // Albedo texture setup
        gAlbedo = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, gAlbedo);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb16f, size.X, size.Y, 0, PixelFormat.Rgb,
            PixelType.Float, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minFilter);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)magFilter);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment2,
            TextureTarget.Texture2D, gAlbedo, 0);
        
        // Material texture setup
        gMaterial = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, gMaterial);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R8i, size.X, size.Y, 0, PixelFormat.RedInteger,
            PixelType.UnsignedByte, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment3,
            TextureTarget.Texture2D, gMaterial, 0);
        
        // Specular texture setup
        gSpecular = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, gSpecular);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, size.X, size.Y, 0, PixelFormat.Rgba,
            PixelType.Float, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minFilter);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)magFilter);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment4,
            TextureTarget.Texture2D, gSpecular, 0);

        // Depth texture setup
        gDepth = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, gDepth);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent, size.X, size.Y, 0,
            PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
            TextureTarget.Texture2D, gDepth, 0);

        // Specify the list of draw buffers
        DrawBuffersEnum[] attachments =
        {
            DrawBuffersEnum.ColorAttachment0,
            DrawBuffersEnum.ColorAttachment1,
            DrawBuffersEnum.ColorAttachment2,
            DrawBuffersEnum.ColorAttachment3,
            DrawBuffersEnum.ColorAttachment4,
        };
        GL.DrawBuffers(attachments.Length, attachments);

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

    public void Bind()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, gBuffer);
        Clear();
    }

    public void Unbind()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Clear()
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public void Render(bool bindShader = true)
    {
        if (bindShader)
            _shader.Use();
        
        Vector2Int size = new Vector2Int(Mathf.RoundToInt(this.size.X * FramebufferScale), Mathf.RoundToInt(this.size.Y * FramebufferScale));
        GL.Viewport(0, 0, size.X, size.Y);
        
        BindTextures();
        
        GL.BindVertexArray(_vao);
        GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0);
        
        GL.Viewport(0, 0, this.size.X, this.size.Y);
    }

    public void BindTextures()
    {
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, gPosition);
        
        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2D, gNormal);
        
        GL.ActiveTexture(TextureUnit.Texture2);
        GL.BindTexture(TextureTarget.Texture2D, gAlbedo);
        
        GL.ActiveTexture(TextureUnit.Texture3);
        GL.BindTexture(TextureTarget.Texture2D, gMaterial);
        
        GL.ActiveTexture(TextureUnit.Texture4);
        GL.BindTexture(TextureTarget.Texture2D, gSpecular);
        
        GL.ActiveTexture(TextureUnit.Texture5);
        GL.BindTexture(TextureTarget.Texture2D, gDepth);
    }

    public void Dispose()
    {
        GL.DeleteFramebuffer(gBuffer);
        GL.DeleteTexture(gPosition);
        GL.DeleteTexture(gNormal);
        GL.DeleteTexture(gAlbedo);
        GL.DeleteTexture(gSpecular);
        GL.DeleteTexture(gDepth);
    }
}