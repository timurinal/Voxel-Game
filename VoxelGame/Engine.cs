using System.Runtime.InteropServices;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using VoxelGame.Maths;
using VoxelGame.Rendering;
using VoxelGame.Rendering.Font;
using Vector2 = VoxelGame.Maths.Vector2;
using Vector3 = VoxelGame.Maths.Vector3;

namespace VoxelGame;

public sealed class Engine : GameWindow
{
    public new bool IsFullscreen { get; set; }
    public new bool IsWireframe { get; set; }

    public readonly Camera Camera;

    internal static int TriangleCount;
    internal static int VertexCount;
    
    private Shader _shader;
    private Chunk[,,] _chunks;

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
        
        TextureAtlas.Init();

        // Make the window visible after setting up so it appears in place and not in a random location
        IsVisible = true;

        _shader = Shader.Load("Shaders/shader.vert", "Shaders/shader.frag");
        _chunks = new Chunk[4, 4, 4];
        Title = $"Generating chunks...";
        for (int x = 0; x < _chunks.GetLength(0); x++)
        {
            for (int y = 0; y < _chunks.GetLength(1); y++)
            {
                for (int z = 0; z < _chunks.GetLength(2); z++)
                {
                    _chunks[x, y, z] = new Chunk(new Vector3Int(x, y, z) * Chunk.ChunkSize, _shader);
                    _chunks[x, y, z].BuildChunk();
                }
            }
        }
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
        TriangleCount = 0;
        VertexCount = 0;
        
        // Render here
        foreach (var chunk in _chunks)
        {
            chunk.Render(Camera);
        }

        Title = $"FPS: {Time.Fps} | Vertices: {VertexCount:N0} Triangles: {TriangleCount:N0}";
        
        SwapBuffers();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        
        GL.Viewport(0, 0, e.Width, e.Height);
        Camera.UpdateProjection(Size);
    }

    [StructLayout(LayoutKind.Sequential)]
    struct VertexData(Vector3 position, Vector2 uv, Colour colour)
    {
        public Vector3 position = position;
        public Vector2 uv = uv;
        public Colour colour = colour;
    }
}