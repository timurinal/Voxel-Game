using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using VoxelGame.Maths;
using VoxelGame.Rendering;
using VoxelGame.Rendering.Font;
using Random = VoxelGame.Maths.Random;
using Vector2 = VoxelGame.Maths.Vector2;
using Vector3 = VoxelGame.Maths.Vector3;

namespace VoxelGame;

public sealed class Engine : GameWindow
{
    public static readonly string DataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VoxelGame");
    
    public new bool IsFullscreen { get; set; }
    public bool IsWireframe { get; set; }

    public const bool EnableFrustumCulling = false;

    public readonly Player Player;

    internal static int TriangleCount;
    internal static int VertexCount;
    internal static int LoadedChunks;
    internal static int VisibleChunks;
    
    private Shader _shader;
    private Dictionary<Vector3Int, Chunk> _chunks;
    private Queue<Vector3Int> _chunksToBuild;
    private const int MaxChunksToBuildPerFrame = 8;

    private Mesh _mesh;
    private Shader _meshShader;

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
        _chunks = new Dictionary<Vector3Int, Chunk>();
        _chunksToBuild = new Queue<Vector3Int>();

        _meshShader = Shader.StandardShader;
        _mesh = new Mesh(_meshShader)
        {
            Vertices =
            [
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f)
            ],
            Triangles =
            [
                0, 2, 1, 3, 2, 0, // front face
                4, 5, 6, 6, 7, 4, // back face
                1, 6, 5, 6, 1, 2, // right face
                0, 4, 7, 7, 3, 0, // left face
                0, 1, 5, 5, 4, 0, // bottom face
                2, 3, 6, 7, 6, 3  // top face
            ],
            Colours = 
            [
                Colour.Red,
                Colour.Green,
                Colour.Blue,
                Colour.Yellow,
                Colour.Cyan,
                Colour.Magenta,
                Colour.White,
                Colour.Black,
            ],
            Transform =
            {
                Position = new Vector3(-5, 0, 0)
            }
        };
    }

    protected override async void OnUpdateFrame(FrameEventArgs args)
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
                    Console.WriteLine($"Chunk at position {chunk.Key} is dirty. Saving chunk...");
                    await SaveChunk(chunk.Key, chunkObj);
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

        if (Input.GetKeyDown(Keys.Space))
        {
            Console.WriteLine("Attempting to rebuild chunk...");
            var chunk = _chunks.First().Value;
            for (int i = 0; i < chunk.voxels.Length; i++)
            {
                chunk.voxels[i] = i % 3 == 0 ? 1u : 0u;
            }
            chunk.RebuildChunk(_chunks);
            Console.WriteLine($"Rebuilt chunk at position {chunk.chunkPosition} (local chunk-space position; {chunk.chunkPosition / Chunk.ChunkSize})");
        }

        for (int x = -Player.ChunkRenderDistance; x < Player.ChunkRenderDistance; x++)
        {
            for (int y = -Player.ChunkRenderDistance; y < Player.ChunkRenderDistance; y++)
            {
                for (int z = -Player.ChunkRenderDistance; z < Player.ChunkRenderDistance; z++)
                {
                    Vector3Int chunkPosition = new Vector3Int(x, y, z) + playerPosition;
                    Vector3Int chunkWorldPosition = chunkPosition * Chunk.ChunkSize;

                    if (!_chunks.ContainsKey(chunkPosition) && chunkWorldPosition.Y > 0)
                    {
                        if (LoadChunk(chunkPosition, out var chunk))
                        {
                            _chunksToBuild.Enqueue(chunkPosition); // Queue the chunk for building
                            _chunks[chunkPosition] = chunk; // Add the chunk to the dictionary
                        }
                        else
                        {
                            var newChunk = new Chunk(chunkWorldPosition, _shader);
                            _chunksToBuild.Enqueue(chunkPosition); // Queue the chunk for building
                            _chunks[chunkPosition] = newChunk; // Add the chunk to the dictionary
                        }
                    }
                }
            }
        }

        // Build a limited number of chunks per frame
        for (int i = 0; i < MaxChunksToBuildPerFrame && _chunksToBuild.Count > 0; i++)
        {
            try
            {
                var chunkPosition = _chunksToBuild.Dequeue();
                _chunks[chunkPosition].BuildChunk(_chunks);
            }
            catch
            {
            }
        }
    }

    private async Task SaveChunk(Vector3Int chunkPosition, Chunk chunk)
    {
        // Ensure directory exists
        var dirPath = Engine.DataPath;
        Directory.CreateDirectory(dirPath);

        // Prepare file path
        var filePath = Path.Combine(dirPath, $"{chunkPosition}.chunk");

        var options = new JsonSerializerOptions
        {
            WriteIndented = true, // use indentation for readability
        };

        // Serialize & save chunk data
        using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, chunk.voxels, options);
        
        chunk.OnSaved();
    }

    private bool LoadChunk(Vector3Int chunkPosition, out Chunk chunk)
    {
        string filePath = Path.Combine(DataPath, $"{chunkPosition}.chunk");

        if (File.Exists(filePath))
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true, // use indentation for readability
            };

            using (var stream = File.OpenRead(filePath))
            {
                var chunkVoxels = JsonSerializer.DeserializeAsync<uint[]>(stream, options).Result;
                chunk = new Chunk(chunkPosition, _shader);
                chunk.voxels = chunkVoxels;
            }

            // Add Chunk to our managed list
            _chunks[chunkPosition] = chunk;
            return true;
        }

        chunk = null;
        return false;
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        TriangleCount = 0;
        VertexCount = 0;
        LoadedChunks = 0;
        VisibleChunks = 0;
        
        // Render here

        try
        {
            foreach (var chunk in _chunks)
            {
                LoadedChunks++;
                if (Player.IsBoxInFrustum(chunk.Value.Bounds) && EnableFrustumCulling)
                {
                    VisibleChunks++;
                    chunk.Value.Render(Player);
                }
                else
                {
                    VisibleChunks++;
                    var c = chunk.Value.Render(Player);
                    VertexCount   += c.vertexCount;
                    TriangleCount += c.triangleCount;
                }
            }
        }
        catch (GLException e) // for some reason, I get an opengl invalid value error when rendering the chunks but my error handler throws an exception when a gl exception is caught. this 'fixes' the issue, it only stops the window closing but chunks all render correctly even after the error is thrown
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(e.Message);
            Console.ResetColor();
        }
        
        var d = _mesh.Render(Player);
        VertexCount   += d.vertexCount;
        TriangleCount += d.triangleCount;

        Title = $"Vertices: {VertexCount:N0} Triangles: {TriangleCount:N0} | Loaded Chunks: {LoadedChunks} Visible Chunks: {VisibleChunks} | FPS: {Time.Fps}";
        
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
