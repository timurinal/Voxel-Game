using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using VoxelGame.Rendering;
using Vector3 = VoxelGame.Maths.Vector3;

namespace VoxelGame;

public sealed class Engine : GameWindow
{
    public new bool IsFullscreen { get; set; }
    public new bool IsWireframe { get; set; }

    public readonly Camera Camera;

    private Shader _shader;
    private int _vao, _vbo, _ebo;

    private int _uniformProjMat;
    private int _uniformViewMat;
    private int _uniformModelMat;
    
    public Engine(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws)
    {
        CenterWindow();

        Camera = new Camera(Size);
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        
        // GL setup
        GL.Enable(EnableCap.Multisample);
        GL.Enable(EnableCap.CullFace);
        GL.Enable(EnableCap.DepthTest);
        
        GL.CullFace(CullFaceMode.Front);
        GL.FrontFace(FrontFaceDirection.Cw);
        
        GL.ClearColor(0.6f, 0.75f, 1f, 1f);

        // Make the window visible after setting up so it appears in place and not in a random location
        IsVisible = true;

        float[] data =
        [
            -0.5f, -0.5f, 0.0f,    1.0f, 0.0f, 0.0f, 1.0f, // Vertex 1: Bottom left corner
             0.5f, -0.5f, 0.0f,    0.0f, 1.0f, 0.0f, 1.0f, // Vertex 2: Bottom right corner
            -0.5f,  0.5f, 0.0f,    0.0f, 0.0f, 1.0f, 1.0f, // Vertex 3: Top left corner
             0.5f,  0.5f, 0.0f,    1.0f, 1.0f, 0.0f, 1.0f, // Vertex 4: Top right corner
        ];

        int[] triangles =
        [
            0, 1, 2,
            2, 1, 3
        ];

        // gen and bind vertex array
        _vao = GL.GenVertexArray();
        GL.BindVertexArray(_vao);
        
        // generate and bind vertex buffer
        _vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float), data, BufferUsageHint.StaticDraw);
        
        // setup vertex attributes
        int stride = 7;
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride * sizeof(float), 0);
        GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, stride * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(1);
        
        // generate and bind element buffer
        _ebo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, triangles.Length * sizeof(int), triangles, BufferUsageHint.StaticDraw);
        
        GL.BindVertexArray(0);
        
        _shader = Shader.Load("Shaders/shader.vert", "Shaders/shader.frag");
        var translation = Matrix4.CreateTranslation(new Vector3(0, 0, -5));
        _shader.SetUniform("m_model", ref translation);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        Input._keyboardState = KeyboardState;
        Time.DeltaTime = (float)args.Time;
        
        Camera.Update(Size);
        
        // Close game window
        if (Input.GetKeyDown(Keys.Escape))
            Close();

        // Toggle fullscreen
        if (Input.GetKeyDown(Keys.F12))
        {
            IsFullscreen = !IsFullscreen;
            WindowState = IsFullscreen ? WindowState.Fullscreen : WindowState.Normal;
        }
        
        // Toggle wireframe
        if (Input.GetKeyDown(Keys.F1))
        {
            IsWireframe = !IsWireframe;
            GL.PolygonMode(MaterialFace.FrontAndBack, IsWireframe ? PolygonMode.Line : PolygonMode.Fill);
        }
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        // Render here
        GL.BindVertexArray(_vao);
        _shader.Use();
        _shader.SetUniform("m_proj", ref Camera.ProjectionMatrix);
        _shader.SetUniform("m_view", ref Camera.ViewMatrix);
        GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0);

        Title = $"FPS: {Time.Fps}";
        
        SwapBuffers();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        
        GL.Viewport(0, 0, e.Width, e.Height);
        Camera.UpdateProjection(Size);
    }
}