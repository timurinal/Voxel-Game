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

    private Vector3 _velocity;

    public Engine(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws)
    {
        CenterWindow();

        Player = new Player(Size);
        CursorState = CursorState.Grabbed;
    }

    protected override async void OnLoad()
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
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        Input._keyboardState = KeyboardState;
        Time.DeltaTime = (float)args.Time;
        
        Vector3Int playerPosition = Vector3.Round(Player.Position / Chunk.ChunkSize);
        int renderDistance = Player.ChunkRenderDistance;

        List<Vector3Int> chunksToRemove = new List<Vector3Int>();

        foreach (var chunk in _chunks)
        {
            var chunkPos = chunk.Key;
            int dx = chunkPos.X - playerPosition.X;
            int dy = chunkPos.Y - playerPosition.Y;
            int dz = chunkPos.Z - playerPosition.Z;

            if (Math.Abs(dx) > renderDistance || Math.Abs(dy) > renderDistance || Math.Abs(dz) > renderDistance)
            {
                var chunkObj = chunk.Value;
                if (chunkObj.IsDirty)
                {
                    // TODO: Implement chunk saving
                }
                chunksToRemove.Add(chunkPos);
            }
        }

        foreach (var chunkPos in chunksToRemove)
        {
            _chunks.Remove(chunkPos);
        }
        
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

        for (int x = -Player.ChunkRenderDistance; x < Player.ChunkRenderDistance; x++)
        {
            for (int y = -Player.ChunkRenderDistance; y < Player.ChunkRenderDistance; y++)
            {
                for (int z = -Player.ChunkRenderDistance; z < Player.ChunkRenderDistance; z++)
                {
                    Vector3Int chunkPosition = new Vector3Int(x, y, z) + playerPosition;
                    Vector3Int chunkWorldPosition = chunkPosition * Chunk.ChunkSize;

                    if (!_chunks.ContainsKey(chunkPosition))
                    {
                        var chunk = new Chunk(chunkWorldPosition, _shader);
                        chunk.BuildChunk(null);
                        _chunks.Add(chunkPosition, chunk);
                    }
                }
            }
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
}