using OpenTK.Mathematics;
using VoxelGame.Maths;

namespace VoxelGame.Rendering;

public enum TexUnit
{
    Texture0 = 0,
    Texture1 = 1,
    Texture2 = 2,
    Texture3 = 3,
    Texture4 = 4,
    Texture5 = 5,
    Texture6 = 6,
    Texture7 = 7,
    Texture8 = 8,
    Texture9 = 9,
    Texture10 = 10,
    Texture11 = 11,
    Texture12 = 12,
    Texture13 = 13,
    Texture14 = 14,
    Texture15 = 15,
    Texture16 = 16,
    Texture17 = 17,
    Texture18 = 18,
    Texture19 = 19,
    Texture20 = 20,
    Texture21 = 21,
    Texture22 = 22,
    Texture23 = 23,
    Texture24 = 24,
    Texture25 = 25,
    Texture26 = 26,
    Texture27 = 27,
    Texture28 = 28,
    Texture29 = 29,
    Texture30 = 30,
    Texture31 = 31,
}

public class SSEffect
{
    private Shader _shader;
    private bool _floating;
    private int _vao, _vbo, _ebo, _fbo, _texture, _depth, _normal;

    public static readonly string DefaultVertexShader = @"
#version 450 core

layout (location = 0) in vec3 vPosition;
layout (location = 1) in vec2 vUv;

out vec2 texcoord;

void main() {
    gl_Position = vec4(vPosition, 1.0);
    
    texcoord = vUv;
}";
    
    public SSEffect(Shader shader, Vector2Int screenSize, bool floating = false)
    {
        _shader = shader;
        _floating = floating;

        float[] data =
        [
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

        const int stride = 5; // each vertex has 3 position floats, and 2 texture coordinates
        
        // setup vertex attributes
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride * sizeof(float), 0 * sizeof(float));
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(1);
        
        // setup element buffer
        _ebo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, triangles.Length * sizeof(int), triangles, BufferUsageHint.StaticDraw);
        
        // unbind vertex array
        GL.BindVertexArray(0);
        
        // setup framebuffer
        _fbo = GL.GenFramebuffer();
        
        // Generate depth texture
        _depth = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _depth);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent32, 
            screenSize.X, screenSize.Y, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
        float[] borderCol = [ 0.0f, 0.0f, 0.0f, 0.0f ];
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, borderCol);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
            TextureTarget.Texture2D, _depth, 0);
        
        // Generate normal texture
        // _normal = GL.GenTexture();
        // GL.BindTexture(TextureTarget.Texture2D, _normal);
        // GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, screenSize.X, screenSize.Y, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
        // GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        // GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        // GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
        // GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
        // GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, borderCol);
        // GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        // GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, _normal, 0);
        
        // Generate colour texture
        if (!floating)
        {
            _texture = GL.GenTexture(); 
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 
                screenSize.X, screenSize.Y, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, _depth, 0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, borderCol);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D, _texture, 0);
        }
        else
        {
            _texture = GL.GenTexture(); 
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, 
                screenSize.X, screenSize.Y, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, _depth, 0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, borderCol);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D, _texture, 0);
        }
        
        // Attach textures to the framebuffer
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
    
        // Attach the depth texture to the framebuffer's depth attachment point
        GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
            _depth, 0);
        
        // Attach the colour texture to the framebuffer's color attachment point
        GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, 
            _texture, 0);
        
        // Attach the normal texture to the framebuffer
        // GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, 
        //     _normal, 0);
    
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void UpdateSize(Vector2Int screenSize)
    {
        GL.DeleteFramebuffer(_fbo);
        GL.DeleteTexture(_depth);
        // GL.DeleteTexture(_normal);
        GL.DeleteTexture(_texture);
        _fbo = GL.GenFramebuffer();
        
        // Generate depth texture
        _depth = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _depth);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent32, 
            screenSize.X, screenSize.Y, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
        float[] borderCol = [ 0.0f, 0.0f, 0.0f, 0.0f ];
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, borderCol);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
            TextureTarget.Texture2D, _depth, 0);
        
        // Generate normal texture
        // _normal = GL.GenTexture();
        // GL.BindTexture(TextureTarget.Texture2D, _normal);
        // GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, screenSize.X, screenSize.Y, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
        // GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        // GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        // GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
        // GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
        // GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, borderCol);
        // GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        // GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, _normal, 0);
        
        // Generate colour texture
        if (!_floating)
        {
            _texture = GL.GenTexture(); 
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 
                screenSize.X, screenSize.Y, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, _depth, 0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, borderCol);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D, _texture, 0);
        }
        else
        {
            _texture = GL.GenTexture(); 
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, 
                screenSize.X, screenSize.Y, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, _depth, 0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, borderCol);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D, _texture, 0);
        }
        
        // Attach textures to the framebuffer
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
    
        // Attach the depth texture to the framebuffer's depth attachment point
        GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
            _depth, 0);
        
        // Attach the color texture to the framebuffer's color attachment point
        GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, 
            _texture, 0);
    
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Use()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
        {
            Console.WriteLine("Error while creating framebuffer");
        }
    }

    public void Unuse()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    internal void Render(Camera camera)
    {
        _shader.Use();
        _shader.SetInt("_MainTex", 0, false);
        _shader.SetInt("_DepthTexture", 1, false);
        
        _shader.SetMatrix("m_proj", ref camera.ProjectionMatrix, autoUse: false);
        _shader.SetMatrix("m_view", ref camera.ViewMatrix, autoUse: false);
        
        Matrix4 m_invProj = Matrix4.Invert(camera.ProjectionMatrix);
        Matrix4 m_invView = Matrix4.Invert(camera.ViewMatrix);
        _shader.SetMatrix("m_invProj", ref m_invProj, autoUse: false);
        _shader.SetMatrix("m_invView", ref m_invView, autoUse: false);
        
        _shader.SetVector3("_CamPos", camera.Position, false);
        
        _shader.SetFloat("NearPlane", camera.NearPlane, false);
        _shader.SetFloat("FarPlane", camera.FarPlane, false);
        GL.BindVertexArray(_vao);
        
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _texture);
        
        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2D, _depth);
        
        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill); // Don't allow the quad to be rendered as wireframe
        
        GL.Enable(EnableCap.DepthTest);
        GL.Clear(ClearBufferMask.DepthBufferBit); // Clear the depth buffer
        GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
        GL.Disable(EnableCap.DepthTest);

        GL.BindVertexArray(0);
    }
}