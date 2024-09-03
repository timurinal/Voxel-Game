using System.Collections.Concurrent;
using System.ComponentModel;
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
    
    public DirectionalLight Sun { get; private set; }

    internal static Dictionary<Vector3Int, Chunk> Chunks = new();

    private Material DefaultChunkMaterial;
    private Shader ChunkDepthShader;
    
    private Texture2D _albedo;
    private Texture2D _normal;
    private Texture2D _specular;
    
    private static ConcurrentQueue<Action> _mainThreadActions = new();
    private const int MaxMainThreadActionCallsPerFrame = 4;
    
    private DeferredRenderBuffer DeferredRenderBuffer;
    private Shader DeferredLightingShader;

    private DeferredPreprocessor SSAO;
    private Shader SSAOShader;

    private static List<Vector3Int> ChunksToBuild = new();
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
        
        GL.Enable(EnableCap.DepthClamp); // idk
        
        GL.Enable(EnableCap.CullFace);  // Allow face culling
        GL.FrontFace(FrontFaceDirection.Cw); // Front face is wound clockwise
        GL.CullFace(CullFaceMode.Back);           // and the back face is culled
        
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha); // Use a pretty simple blending function for transparency
        
        // Set the clear colour to a light-blue
        Colour clearCol = Colour.ConvertBase(153, 191, 255);
        GL.ClearColor(clearCol);
        
        // Initialise the camera
        Camera = new Camera();

        DeferredLightingShader = Shader.Load("assets/shaders/deferred.vert", "assets/shaders/deferred.frag");
        DeferredRenderBuffer = new DeferredRenderBuffer(DeferredLightingShader, Size);
        
        DeferredLightingShader.SetInt("shadowMap", 6);
        DeferredLightingShader.SetInt("gAo", 7);

        SSAOShader = Shader.Load("assets/shaders/deferred.vert", "assets/shaders/ssao.frag");
        SSAOShader.Use();
        SSAOShader.SetInt("gPosition", 0, autoUse: false);
        SSAOShader.SetInt("gNormal"  , 1, autoUse: false);
        SSAOShader.SetInt("gAlbedo"  , 2, autoUse: false);
        SSAOShader.SetInt("gSpecular", 3, autoUse: false);
        SSAOShader.SetInt("gDepth"   , 4, autoUse: false);
        SSAO = new DeferredPreprocessor(SSAOShader, Size);

        Sun = new DirectionalLight();
        Sun.LightPosition = new Vector3(1f, 2f, 1f);

        _albedo = new Texture2D("assets/textures/atlas-main.png", useLinearSampling: false, anisoLevel: 16, generateMipmaps: true);
        _normal = new Texture2D("assets/textures/atlas-normal.png", useLinearSampling: false, anisoLevel: 16, generateMipmaps: true);
        _specular = new Texture2D("assets/textures/atlas-specular.png", useLinearSampling: false, anisoLevel: 16, generateMipmaps: true);
        Shader shader = Shader.Load("assets/shaders/chunk-shader.vert", "assets/shaders/chunk-shader-gpass.frag");
        DefaultChunkMaterial = new Material(shader);

        ChunkDepthShader = Shader.Load("assets/shaders/depth.vert", "assets/shaders/depth.frag");
        
        shader.SetInt("Albedo", 0);
        shader.SetInt("Normal", 1);
        shader.SetInt("Specular", 2);
        
        shader.SetFloat("Near", Camera.NearPlane);
        shader.SetFloat("Far", Camera.FarPlane);
        
        shader.SetVector2("screenSize", new(Size.X, Size.Y));
        
        // Make the window visible after all setup has been completed
        IsVisible = true;
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
            var chunkToRemove = Chunks[chunkPos];
            chunkToRemove.Dispose(); // Dispose any vertex arrays related to this chunk to free up gpu memory
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
        
        Sun.UpdateMatrix(Camera.Position);
        
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
                            ChunksToBuild.Add(chunkPosition); // Queue the chunk for building
                        }
                    }
                }
            }
        }
        
        for (int i = 0; i < MaxChunksToBuildPerFrame && ChunksToBuild.Count > 0; i++)
        {
            var first = ChunksToBuild[0];
            ChunksToBuild.RemoveAt(0);
            var chunkPosition = first;
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
        
        Title = $"v0.0.0 - OpenGL4 (Deferred Renderer) | Vertices: {VertexCount:N0} Triangles: {TriangleCount:N0} | FPS: {_fps}";

        Time.ElapsedTime += Time.DeltaTime;
        Time.UpdateFps();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        VertexCount = 0;
        TriangleCount = 0;
        
        // Render here
        // GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        if (Sun.Shadows)
            Render(0);
        
        DeferredRenderBuffer.Bind();
        DeferredRenderBuffer.Clear();
        
        GL.PolygonMode(MaterialFace.FrontAndBack, IsWireframe ? PolygonMode.Line : PolygonMode.Fill);
        
        Render(1);
        
        DeferredRenderBuffer.Unbind();
        
        // Ensure fill mode is used when rendering the deferred quad
        // This is so I can still use wireframe mode without just seeing a quad across the whole screen
        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        
        DeferredRenderBuffer.BindTextures();
        
        SSAOShader.Use();
        SSAOShader.SetMatrix("m_proj", ref Camera.ProjectionMatrix, autoUse: false);
        SSAO.Render(bindShader: false);
        
        // Clear the colour and depth buffers
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        GL.ActiveTexture(TextureUnit.Texture6);
        GL.BindTexture(TextureTarget.Texture2D, SSAO.Texture);
        
        DeferredLightingShader.Use();
        DeferredLightingShader.SetInt("ShadowsEnabled", Sun.Shadows ? 1 : 0, autoUse: false);
        DeferredLightingShader.SetInt("SoftShadows", Sun.SoftShadows ? 1 : 0, autoUse: false);
        
        DeferredLightingShader.SetMatrix("m_proj", ref Camera.ProjectionMatrix, autoUse: false);
        DeferredLightingShader.SetMatrix("m_view", ref Camera.ViewMatrix, autoUse: false);
        DeferredLightingShader.SetMatrix("m_invView", ref Camera.InvViewMatrix, autoUse: false);

        var sunLightProjMatrix = Sun.LightProjMatrix;
        var sunLightViewMatrix = Sun.LightViewMatrix;
        DeferredLightingShader.SetMatrix("m_lightProj", ref sunLightProjMatrix, autoUse: false);
        DeferredLightingShader.SetMatrix("m_lightView", ref sunLightViewMatrix, autoUse: false);
        
        DeferredLightingShader.SetVector3("LightDir", Sun.LightDirection, autoUse: false);
        
        DeferredLightingShader.SetVector3("viewPos", Camera.Position, autoUse: false);
        
        GL.ActiveTexture(TextureUnit.Texture6);
        GL.BindTexture(TextureTarget.Texture2D, Sun.DepthTexture);
        
        DeferredRenderBuffer.Render(bindShader: false);
        
        // Finally, swap buffers
        SwapBuffers();
    }

    /// <summary>
    /// Renders the scene with a specific mode
    /// </summary>
    /// <param name="renderMode">0 = Shadow pass, 1 = Geometry pass</param>
    /// <exception cref="ArgumentException">Thrown when <see cref="renderMode"/> is not 0 or 1</exception>
    private void Render(int renderMode)
    {
        if (renderMode == 0)
        {
            Sun.Use();
            
            ChunkDepthShader.Use();
            var sunLightProjMatrix = Sun.LightProjMatrix;
            var sunLightViewMatrix = Sun.LightViewMatrix;
            ChunkDepthShader.SetMatrix("m_lightProj", ref sunLightProjMatrix, autoUse: false);
            ChunkDepthShader.SetMatrix("m_lightView", ref sunLightViewMatrix, autoUse: false);
            
            foreach (var chunk in Chunks)
            {
                if (!chunk.Value.IsEmpty)
                {
                    // if (Sun.Frustum.IsBoundingBoxInFrustum(chunk.Value.Bounds))
                    {
                        chunk.Value.DirLightRender(ChunkDepthShader, Sun);
                    }
                }
            }
            
            Sun.Unuse(Size);
        }
        else if (renderMode == 1)
        {
            _albedo.Use();
            _normal.Use(TextureUnit.Texture1);
            _specular.Use(TextureUnit.Texture2);
            DefaultChunkMaterial.Shader.Use();
            //DefaultChunkMaterial.Shader.SetVector3("viewPos", Camera.Position, autoUse:false);

            DefaultChunkMaterial.Shader.SetMatrix("m_proj", ref Camera.ProjectionMatrix);
            DefaultChunkMaterial.Shader.SetMatrix("m_view", ref Camera.ViewMatrix);
            
            foreach (var chunk in Chunks)
            {
                if (!chunk.Value.IsEmpty)
                {
                    if (Camera.Frustum.IsBoundingBoxInFrustum(chunk.Value.Bounds))
                    {
                        chunk.Value.Render(false);
                    }
                }
            }
        }
        else
        {
            throw new AggregateException($"Render mode invalid ({renderMode}).");
        }
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        // Update the GL viewport
        GL.Viewport(0, 0, e.Width, e.Height);
        
        // Update the camera's projection with the new screen size
        Camera.UpdateProjection(e.Size);
        
        DefaultChunkMaterial.Shader.SetVector2("screenSize", new(e.Size.X, e.Size.Y));
        
        DeferredRenderBuffer.Resize(e.Size);
        
        SSAO.Resize(e.Size);
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

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        
        GL.DeleteTexture(_albedo.GetHandle());
        GL.DeleteTexture(_normal.GetHandle());
        GL.DeleteTexture(_specular.GetHandle());
        SSAO.Dispose();
        DeferredRenderBuffer.Dispose();
    }
}
