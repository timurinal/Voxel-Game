using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using VoxelGame.Common;
using VoxelGame.Maths;
using VoxelGame.Graphics;
using VoxelGame.Graphics.Shaders;
using VoxelGame.Threading;
using ErrorCode = OpenTK.Graphics.OpenGL4.ErrorCode;

namespace VoxelGame;

public sealed class Engine : GameWindow
{
    public static Camera Camera { get; private set; }

    public static int VertexCount { get; internal set; }
    public static int TriangleCount { get; internal set; }
    
    public static bool IsWireframe { get; private set; }

    internal static Dictionary<Vector3Int, Chunk> Chunks = new();

    private Material DefaultChunkMaterial;
    
    private Texture2D _texture;
    private Texture2D _normal;
    
    private static ConcurrentQueue<Action> _mainThreadActions = new();
    private const int MaxMainThreadActionCallsPerFrame = 4;
    

    private static SortedList<(float dst, int hash), Vector3Int> ChunksToBuild = new();
    private const int MaxChunksToBuildPerFrame = 8;
    

    private int _fpsTotal;
    private const int FpsUpdatedPerSecond = 3;
    private float _nextFpsUpdateTime;
    private float _fps = 0;
    
    public Engine(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws)
    {
        // Center the window
        // The window starts not visible so the window can be centered without looking like its jumping around
        CenterWindow();

        // Lock the cursor in the middle of the screen
        CursorState = CursorState.Grabbed;
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        
        // GL setup should be done first
        
        GL.Enable(EnableCap.DepthTest); // Allow depth testing
        
        GL.Enable(EnableCap.CullFace);  // Allow face culling
        GL.FrontFace(FrontFaceDirection.Cw); // Front face is wound clockwise
        GL.CullFace(CullFaceMode.Back);           // and the back face is culled
        
        GL.Enable(EnableCap.Multisample); // Allow MSAA
        
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha); // Use a pretty simple blending function for transparency
        
        // Set the clear colour to a light-blue
        Colour clearCol = Colour.ConvertBase(153, 191, 255);
        GL.ClearColor(clearCol);
        
        // Initialise the camera
        Camera = new Camera();
        
        // Make the window visible after all GL setup has been completed
        IsVisible = true;

        _texture = new Texture2D("assets/textures/atlas-main.png", useLinearSampling: false, anisoLevel: 16, generateMipmaps: true);
        _normal = new Texture2D("assets/textures/atlas-normal.png", useLinearSampling: false, anisoLevel: 16, generateMipmaps: true);
        Shader shader = Shader.Load("assets/shaders/chunk-shader.vert", "assets/shaders/chunk-shader.frag");
        DefaultChunkMaterial = new Material(shader);
        
        shader.SetInt("TestTexture", 0);
        shader.SetInt("Normal", 1);
        
        shader.SetFloat("Near", Camera.NearPlane);
        shader.SetFloat("Far", Camera.FarPlane);
        
        shader.SetVector2("screenSize", new(Size.X, Size.Y));
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        
        // Update loop

        Time.DeltaTime = (float)args.Time;
        Time.NumFrames++;
        Input._kbState = KeyboardState;

        // if there are any actions registerd to be run on the main thread, run them
        for (int i = 0; i < MaxMainThreadActionCallsPerFrame && _mainThreadActions.Count > 0; i++)
        {
            if (_mainThreadActions.TryDequeue(out var action))
            {
                action();
            }
        }
        // var copy = new List<Action>(_mainThreadActions);
        // _mainThreadActions.Clear();
        // foreach (var action in copy)
        // {
        //     action();
        // }
        
        Vector3Int playerPosition = Vector3.Round(Camera.Position / Chunk.ChunkSize);
        
        List<Vector3Int> chunksToRemove = new List<Vector3Int>();
        
        foreach (var chunk in Chunks)
        {
            var chunkPos = chunk.Key;
            if (Vector3Int.SqrDistance(chunkPos, playerPosition) > PlayerSettings.SqrRenderDistance)
            {
                chunksToRemove.Add(chunkPos);
            }
        }
        
        Vector3Int[] neighbourChunkOffsets =
        [
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0)
        ];
        
        foreach (var chunkPos in chunksToRemove)
        {
            Chunks.Remove(chunkPos);
        }
        
        // Close the window when the escape key is pressed
        if (Input.GetKeyDown(Keys.Escape))
            Close();
        
        // Toggle wireframe when the f1 key is pressed
        if (Input.GetKeyDown(Keys.F1))
            IsWireframe = !IsWireframe;
        
        // Update the camera
        Camera.Update();
        
        for (int x = -PlayerSettings.RenderDistance; x < PlayerSettings.RenderDistance; x++)
        {
            for (int y = -PlayerSettings.RenderDistance; y < PlayerSettings.RenderDistance; y++)
            {
                for (int z = -PlayerSettings.RenderDistance; z < PlayerSettings.RenderDistance; z++)
                {
                    Vector3Int chunkPosition = new Vector3Int(x, y, z) + playerPosition;
                    // Vector3Int chunkCenter = chunkPosition + (Vector3Int.One * Chunk.HChunkSize);
                    
                    // Check if the chunk is within the spherical render distance
                    if (Vector3Int.SqrDistance(chunkPosition, playerPosition) > PlayerSettings.SqrRenderDistance) continue;

                    Vector3Int chunkWorldPosition = chunkPosition * Chunk.ChunkSize;
                    
                    if (!Chunks.ContainsKey(chunkPosition) && chunkWorldPosition.Y >= 0)
                    {
                        var newChunk = new Chunk(chunkPosition, DefaultChunkMaterial);
                        newChunk.BuildChunk();

                        Chunks[chunkPosition] = newChunk;
                        
                        if (!newChunk.IsEmpty)
                        {
                            float sqrDst = Vector3.SqrDistance(newChunk.Centre, Camera.Position);

                            int hash = chunkPosition.GetHashCode();
                            
                            ChunksToBuild.Add((sqrDst, hash), chunkPosition); // Queue the chunk for building
                        }
                    }
                }
            }
        }
        
        for (int i = 0; i < MaxChunksToBuildPerFrame && ChunksToBuild.Count > 0; i++)
        {
            var firstKey = ChunksToBuild.Keys[0];
            var firstValue = ChunksToBuild[firstKey];
            ChunksToBuild.RemoveAt(0);
            var chunkPosition = firstValue;
            if (Chunks.TryGetValue(chunkPosition, out var chunk))
            {
                chunk.BuildChunk();
        
                // Force neighbouring chunks to rebuild
                foreach (var offset in neighbourChunkOffsets)
                {
                    if (Chunks.TryGetValue(chunkPosition + offset, out var neighbour))
                    {
                        neighbour.BuildChunk();
                    }
                }
            }
        }

        if (Time.ElapsedTime >= _nextFpsUpdateTime)
        {
            _fps = Time.Fps;
            _nextFpsUpdateTime = Time.ElapsedTime + (1f / FpsUpdatedPerSecond);
        }
        
        Title = $"Vertices: {VertexCount:N0} Triangles: {TriangleCount:N0} | FPS: {_fps} | {Vector3.Round(Camera.Position, 3)}";

        Time.ElapsedTime += Time.DeltaTime;
        Time.UpdateFps();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        VertexCount = 0;
        TriangleCount = 0;
        
        // Render loop
        
        // Clear the colour and depth buffers
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        GL.PolygonMode(MaterialFace.FrontAndBack, IsWireframe ? PolygonMode.Line : PolygonMode.Fill);
        
        // Render here
        _texture.Use();
        _normal.Use(TextureUnit.Texture1);
        DefaultChunkMaterial.Shader.Use();
        DefaultChunkMaterial.Shader.SetVector3("viewPos", Camera.Position, autoUse:false);

        foreach (var chunk in Chunks)
        {
            if (!chunk.Value.IsEmpty)
            {
                if (Camera.Frustum.IsBoundingBoxInFrustum(chunk.Value.Bounds))
                {
                    chunk.Value.Render(Camera, false);
                }
            }
        }
        
        // Finally, swap buffers
        SwapBuffers();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        // Update the GL viewport
        GL.Viewport(0, 0, e.Width, e.Height);
        
        // Update the camera's projection with the new screen size
        Camera.UpdateProjection(e.Size);
        
        DefaultChunkMaterial.Shader.SetVector2("screenSize", new(e.Size.X, e.Size.Y));
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);
        
        // Update the camera's rotation with the mouse delta
        Camera.Rotate(e.Delta);
    }

    public static void RunOnMainThread(Action action)
    {
        _mainThreadActions.Enqueue(action);
    }
}
