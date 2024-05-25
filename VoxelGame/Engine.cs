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

    public readonly Player Player;

    internal static int TriangleCount;
    internal static int VertexCount;
    
    private Shader _shader;
    private Dictionary<Vector3Int, Chunk> _chunks;
    private List<AABB> _collisions;

    private Vector3 _velocity;

    public Engine(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws)
    {
        CenterWindow();

        Player = new Player(Size);
        CursorState = CursorState.Grabbed;
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
        _chunks = new();
        _collisions = new();
        Title = $"Generating chunks...";
        // precompute the voxels for the chunk so faces can be culled between chunks
        const int worldSize = 4;
        for (int x = 0; x < worldSize; x++)
        {
            for (int y = 0; y < worldSize; y++)
            {
                for (int z = 0; z < worldSize; z++)
                {
                    _chunks.Add(new(x, y, z), new Chunk(new Vector3Int(x, y, z) * Chunk.ChunkSize, _shader));
                }
            }
        }
        for (int x = 0; x < worldSize; x++)
        {
            for (int y = 0; y < worldSize; y++)
            {
                for (int z = 0; z < worldSize; z++)
                {
                    _chunks[new(x, y, z)].BuildChunk(_chunks);
                    _collisions.AddRange(_chunks[new(x, y, z)].GenerateCollisions());
                }
            }
        }
        
        Console.WriteLine($"{_collisions.Count:N0} collision AABBs generated.");
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        Input._keyboardState = KeyboardState;
        Time.DeltaTime = (float)args.Time;
        
        Player.Update(Size);
        
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
            // float chunkDistanceSqr = (chunk.Value.chunkPosition - Player.Position).SqrMagnitude;
            // float renderDistanceSqr = Player.ChunkRenderDistance * Player.ChunkRenderDistance * Chunk.ChunkSize;
            // if (chunkDistanceSqr > renderDistanceSqr)
            //     continue;
            chunk.Value.Render(Player);
        }

        Title = $"FPS: {Time.Fps} | Vertices: {VertexCount:N0} Triangles: {TriangleCount:N0}";
        
        SwapBuffers();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        
        GL.Viewport(0, 0, e.Width, e.Height);
        Player.UpdateProjection(Size);
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);
        
        Player.Rotate(e.DeltaX, e.DeltaY);
    }

    [StructLayout(LayoutKind.Sequential)]
    struct VertexData(Vector3 position, Vector2 uv, Colour colour)
    {
        public Vector3 position = position;
        public Vector2 uv = uv;
        public Colour colour = colour;
    }
}