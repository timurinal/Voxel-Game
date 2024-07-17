// 6,953 lines of code :D

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using ImGuiNET;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using VoxelGame.Maths;
using VoxelGame.Rendering;
using Random = VoxelGame.Maths.Random;
using Vector2 = VoxelGame.Maths.Vector2;
using Vector3 = VoxelGame.Maths.Vector3;

namespace VoxelGame;

public enum RenderMode
{
    Polygon = 0,
    RayTraced = 1
}

public sealed class Engine : GameWindow
{
    public static readonly string DataPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "VoxelGame");
    
    public bool ShowGui { get; set; }

    public bool IsFullscreen;
    public bool IsWireframe;
    public bool Shadows = true;
    public bool FrustumCulling = true;
    public RenderMode RenderMode = RenderMode.Polygon;


    public readonly Player Player;
    
    public static Thread MainThread { get; private set; }
    public static Thread CurrentThread => Thread.CurrentThread;
    public static bool IsMainThread => MainThread.ManagedThreadId == CurrentThread.ManagedThreadId;

    internal static List<PointLight> Lights = new();

    internal static int TriangleCount;
    internal static int VertexCount;
    internal static int BatchCount;
    internal static int LoadedChunks;
    internal static int VisibleChunks;

    public static int MaxFPS;

    internal ImGuiController _imGuiController;

    internal readonly ShadowMapper ShadowMapper;
    private Shader _depthShader;
    
    private Shader _chunkShader;
    internal static Dictionary<Vector3Int, Chunk> Chunks;
    private SortedList<float, Vector3Int> _chunksToBuild;
    private const int MaxChunksToBuildPerFrame = 16;

    private Shader _meshShader;

    private Mesh _test;
    
    private readonly Vector3 _lightColour = new Vector3(1.0f, 0.898f, 0.7f);
    // private readonly Vector3 _lightColour = new Vector3(1.0f, 1.0f, 1.0f);

    private bool _newLightThisFrame = true; // this is true when a (or multiple) new light is added to the scene. The light buffer is recalculated when this is true

    private int _lightBuffer;

    private Vector3 _lightDir = new Vector3(-2f, -2f, -0.5f);
    // private Vector3 _lightDir = new Vector3(-1, -1, 0);

    private string[] _imGuiChunkBuilderDropdown =
    [
        "Not Blocking",
        "Fully blocking"
    ];

    public static int ChunkBuilderMode = 0;

    private const float FpsUpdateRate = 1.5f;
    private float _nextFpsUpdateTime = 0;
    private float _deltaTime;

    private SSEffect _tonemapper;
    private SSEffect _skybox;
    
    private SSEffect _raytracing;
    private SSEffect _denoiser;
    
    private Shader _tonemapperShader;
    private Shader _skyboxShader;
    
    private Shader _raytracingShader;
    private Shader _denoiserShader;

    private Texture2D _testTexture;

    private ShaderStorageBuffer _rtVoxelDataStorageBuffer;
    private ShaderStorageBuffer _rtVoxelStorageBuffer;

    private static readonly ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();

    private GizmoBox box;
    
    private readonly object _syncObj = new object();

    public Engine(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws)
    {
        if (nws.NumberOfSamples != 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("MSAA is not supported and will not be applied.");
            Console.ResetColor();
        }
        
        ShadowMapper = new();
        CenterWindow();

        Player = new Player(Size);
        
        MainThread = Thread.CurrentThread;
    }

    protected override async void OnLoad()
    {
        base.OnLoad();

        // GL setup
        GL.Enable(EnableCap.CullFace);
        GL.Enable(EnableCap.DepthTest);

        GL.CullFace(CullFaceMode.Back);
        GL.FrontFace(FrontFaceDirection.Cw);
        
        GL.DepthFunc(DepthFunction.Less);

        GL.ClearColor(0.6f, 0.75f, 1f, 1.0f);
        
        // Initialise ImGUI
        _imGuiController = new ImGuiController(Size.X, Size.Y);
        // load any imgui fonts here

        // Make the window visible after setting up so it appears in place and not in a random location
        IsVisible = true;

        // Load all the shaders

        var assetLoadTimer = new System.Diagnostics.Stopwatch();
        assetLoadTimer.Start();
        
        Console.WriteLine("Loading assets...");
        
        TextureAtlas.Init();
        FontAtlas.Init();
        
        _depthShader = Shader.LoadFromAssembly("VoxelGame.Assets.Shaders.depth.vert", "VoxelGame.Assets.Shaders.depth.frag");
        _chunkShader = Shader.LoadFromAssembly("VoxelGame.Assets.Shaders.chunk-shader.vert", "VoxelGame.Assets.Shaders.chunk-shader.frag");
        _tonemapperShader = Shader.LoadFromAssembly("BUILTIN.image-effect.vert", "VoxelGame.Assets.Shaders.tonemapper.frag");
        _skyboxShader = Shader.LoadFromAssembly("VoxelGame.Assets.Shaders.skybox.vert", "VoxelGame.Assets.Shaders.skybox.frag");
        _raytracingShader = Shader.LoadFromAssembly("VoxelGame.Assets.Shaders.raytracer.vert", "VoxelGame.Assets.Shaders.raytracer.frag");
        _denoiserShader = Shader.LoadFromAssembly("BUILTIN.image-effect.vert", "VoxelGame.Assets.Shaders.denoiser.frag");
        
        _testTexture = Texture2D.LoadFromAssembly("VoxelGame.Assets.Textures.uv-checker.png", useLinearSampling: true, generateMipmaps: true);

        assetLoadTimer.Stop();
        Console.WriteLine($"Loaded all assets in {assetLoadTimer.ElapsedMilliseconds}ms");

        _chunkShader.SetUniform("material.diffuse", 0);
        _chunkShader.SetUniform("material.specular", 1);
        _chunkShader.SetUniform("material.shininess", 128.0f);

        Vector3 ambientLighting = Vector3.One * 0.4f;
        DirLight dirLight = new(_lightDir, ambientLighting, Vector3.One * 1f, _lightColour);
        _chunkShader.SetUniform("dirLight.direction", dirLight.direction);
        _chunkShader.SetUniform("dirLight.ambient"  , dirLight.ambient);
        _chunkShader.SetUniform("dirLight.diffuse"  , dirLight.diffuse);
        _chunkShader.SetUniform("dirLight.specular" , dirLight.specular);
        
        _tonemapper = new SSEffect(_tonemapperShader, Size, true);
        
        _skyboxShader.Use();
        _skyboxShader.SetUniform("SkyColourZenith", new Vector3(0.5019608f, 0.67058825f, 0.8980393f), autoUse: false);
        _skyboxShader.SetUniform("SkyColourHorizon", new Vector3(1, 1, 1), autoUse: false);
        // _skyboxShader.SetUniform("GroundColour", new Vector3(0.5647059f, 0.5254902f, 0.5647059f), autoUse: false);
        _skyboxShader.SetUniform("GroundColour", new Vector3(0.08f, 0.08f, 0.08f), autoUse: false);
        _skyboxShader.SetUniform("SunColour", _lightColour, autoUse: false);
        
        _skyboxShader.SetUniform("SunFocus", 15000f);
        _skyboxShader.SetUniform("SunIntensity", 500f);
        
        _skyboxShader.SetUniform("SunLightDirection", _lightDir);
        
        _skybox = new SSEffect(_skyboxShader, Size, true);
        
        _raytracingShader.Use();
        _raytracingShader.SetUniform("MaxLightBounces", RayTracing.MaxLightBounces, autoUse: false);
        _raytracingShader.SetUniform("RaysPerPixel", RayTracing.RaysPerPixel, autoUse: false);
        _raytracingShader.SetUniform("SkyboxIntensity", RayTracing.SkyboxIntensity, autoUse: false);
        
        _raytracingShader.SetUniform("SkyColourZenith", new Vector3(0.5019608f, 0.67058825f, 0.8980393f), autoUse: false);
        _raytracingShader.SetUniform("SkyColourHorizon", new Vector3(1, 1, 1), autoUse: false);
        // _raytracingShader.SetUniform("GroundColour", new Vector3(0.5647059f, 0.5254902f, 0.5647059f), autoUse: false);
        _raytracingShader.SetUniform("GroundColour", new Vector3(0.08f, 0.08f, 0.08f), autoUse: false);
        
        _raytracingShader.SetUniform("SunFocus", 15000f);
        _raytracingShader.SetUniform("SunSize", 500f);
        
        _raytracingShader.SetUniform("SunLightDirection", _lightDir);
        
        _raytracing = new SSEffect(_raytracingShader, Size, true);

        _denoiser = new SSEffect(_denoiserShader, Size, true);
        
        Chunks = new Dictionary<Vector3Int, Chunk>();
        _chunksToBuild = new();

        _lightBuffer = GL.GenBuffer();

        _test = MeshUtility.GenerateCube(Shader.StandardShader);
        _test.Transform.Scale = Vector3.One * 0.05f;
        for (int i = 0; i < _test.Colours.Length; i++)
        {
            _test.Colours[i] = Colour.Yellow;
        }

        _rtVoxelDataStorageBuffer = new ShaderStorageBuffer(0);
        _rtVoxelStorageBuffer = new ShaderStorageBuffer(1);

        ShaderVoxelData[] svd = new ShaderVoxelData[VoxelData.Voxels.Length];

        for (int i = 0; i < svd.Length; i++)
        {
            VoxelData.Voxel voxelData = VoxelData.Voxels[i];
            svd[i] = new ShaderVoxelData(voxelData.id, voxelData.textureFaces);
        }
        
        _rtVoxelDataStorageBuffer.SetData(Marshal.SizeOf<ShaderVoxelData>(), svd);

        AABB bounds = new AABB(Vector3.Zero);
        box = new GizmoBox(bounds);

        // const float atlasWidthInPixels = FontAtlas.FontAtlasWidth;
        // const float additionalSpacing = 0.8f; // Adjust this value as per your needs
        // float xOffset = 0;
        //
        // for (int i = 0; i < 5; i++)
        // {
        //     FontAtlas.CharacterSet.Character character;
        //
        //     if (i == 0) character = FontAtlas.MainCharacterSet.GetCharacter('H');
        //     else if (i == 1) character = FontAtlas.MainCharacterSet.GetCharacter('e');
        //     else if (i == 2) character = FontAtlas.MainCharacterSet.GetCharacter('l');
        //     else if (i == 3) character = FontAtlas.MainCharacterSet.GetCharacter('l');
        //     else if (i == 4) character = FontAtlas.MainCharacterSet.GetCharacter('o');
        //     else character = FontAtlas.CharacterSet.MissingCharacter;
        //
        //     float characterWidthRelative = character.Width / atlasWidthInPixels;
        //
        //     UIRenderer.CreateQuad(new(xOffset, 0, 0), Vector3.One, true, character.Id);
        //
        //     xOffset += characterWidthRelative + additionalSpacing;
        // }
    }

    private bool _updated = false;
    protected override async void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        UpdateFrequency = MaxFPS;

        Input._keyboardState = KeyboardState;
        Time.DeltaTime = (float)args.Time;

        // Run any main thread actions that have been registered from async functions
        Action actionToExecute;

        while (_mainThreadActions.TryDequeue(out actionToExecute)) 
        {
            try
            {
                actionToExecute();
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch (Exception e)
            {
            }
        }
        
        Vector3Int playerPosition = Vector3.Round(Player.Position / Chunk.ChunkSize);
        int renderDistance = Player.ChunkRenderDistance;
        int sqrRenderDistance = Player.SqrChunkRenderDistance;

        List<Vector3Int> chunksToRemove = new List<Vector3Int>();
        
        foreach (var chunk in Chunks)
        {
            var chunkPos = chunk.Key;
            if (Vector3Int.SqrDistance(chunkPos, playerPosition) > sqrRenderDistance)
            {
                // TODO: Save dirty chunks to the disk
                chunksToRemove.Add(chunkPos);
            }
        }
        
        foreach (var chunkPos in chunksToRemove)
        {
            Chunks.Remove(chunkPos);
        }
        
        CursorState = ShowGui ? CursorState.Normal : CursorState.Grabbed;

        Vector3Int playerChunkPosition = Vector3Int.FloorToInt(Player.Position / Chunk.ChunkSize);

        List<AABB> collisions = new();
        
        Vector3Int[] neighbourChunkOffsets =
        {
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
        };
        
        // if (Chunks.TryGetValue(playerChunkPosition, out var currentChunk))
        //     collisions.AddRange(currentChunk.GenerateCollisions(Chunks));
        //
        // foreach (var chunkOffset in neighbourChunkOffsets)
        // {
        //     if (Chunks.TryGetValue(playerChunkPosition + chunkOffset, out var chunk))
        //     {
        //         collisions.AddRange(chunk.GenerateCollisions(Chunks));
        //     }
        // }
        
        Player.Update(Size, null);
        
        if (Input.GetKeyDown(Keys.Escape))
            Close();

        // Toggle fullscreen
        if (Input.GetKeyDown(Keys.F12))
        {
            IsFullscreen = !IsFullscreen;
            WindowState = IsFullscreen ? WindowState.Fullscreen : WindowState.Normal;
        }
        
        // Toggle wireframe
        if (Input.GetKeyDown(Keys.F1)) IsWireframe = !IsWireframe;
        if (Input.GetKeyDown(Keys.F2)) ShowGui = !ShowGui;

        if (Input.GetKeyDown(Keys.F3)) Shadows = !Shadows;

        if (Input.GetKeyDown(Keys.Space))
        {
            _updated = false;
            
            var chunk = Chunks[Vector3Int.Zero];
            for (int i = 0; i < chunk.voxels.Length; i++)
            {
                chunk.voxels[i] = chunk.voxels[i] == 4u ? 4u : 0u;
            }
            chunk.RebuildChunk(Chunks, recursive: true);
        }

        if (Input.GetKeyDown(Keys.Enter))
        {
            Lights.Add(new PointLight(Player.Position, 50f, 1.5f, Vector3.Zero, _lightColour, _lightColour));
            RecalculateLightBuffer(Lights.Count);
        }

        for (int x = -Player.ChunkRenderDistance; x <= Player.ChunkRenderDistance; x++)
        {
            for (int y = -Player.ChunkRenderDistance; y <= Player.ChunkRenderDistance; y++)
            {
                for (int z = -Player.ChunkRenderDistance; z <= Player.ChunkRenderDistance; z++)
                {
                    Vector3Int chunkPosition = new Vector3Int(x, y, z) + playerPosition;

                    // Check if the chunk is within the spherical render distance
                    if (Vector3Int.SqrDistance(chunkPosition, playerPosition) > sqrRenderDistance) continue;
                    
                    Vector3Int chunkWorldPosition = chunkPosition * Chunk.ChunkSize;

                    if (!Chunks.ContainsKey(chunkPosition) && chunkWorldPosition.Y >= 0)
                    {
                        // TODO: Load chunks from disk
                        var newChunk = new Chunk(chunkWorldPosition, _chunkShader);

                        if (!newChunk.IsEmpty)
                        {
                            float sqrDst = Vector3.SqrDistance(newChunk.chunkCentre, Player.Position);

                            // If there is already a chunk with the same distance,
                            // keep adding a small value to it until that key isn't in the list
                            // To prevent an infinite loop, only run this code a maximum of 100 times.
                            // But to ensure that the new key actually is different, the amount added increases
                            // with each iteration
                            int i = 0;
                            const int maxIterations = 100;
                            do
                            {
                                sqrDst += 0.01f * (i / 10f);
                                i++;
                            } while (_chunksToBuild.ContainsKey(sqrDst) && i < maxIterations);
                            
                            _chunksToBuild.Add(sqrDst, chunkPosition); // Queue the chunk for building
                            Chunks[chunkPosition] = newChunk; // Add the chunk to the dictionary
                        }
                    }
                }
            }
        }

        // Build a limited number of chunks per frame
        if (RenderMode == RenderMode.Polygon)
        {
            for (int i = 0; i < MaxChunksToBuildPerFrame && _chunksToBuild.Count > 0; i++)
            {
                try
                {
                    var firstKey = _chunksToBuild.Keys[0];
                    var firstValue = _chunksToBuild[firstKey];
                    _chunksToBuild.RemoveAt(0);
                    var chunkPosition = firstValue;
                    var chunk = Chunks[chunkPosition];

                    // await chunk.GenerateChunkAsync(Chunks);
                    // chunk.RebuildChunk(Chunks, recursive: true);
                    chunk.BuildChunk(Chunks);
        
                    // Force neighbouring chunks to rebuild
                    foreach (var offset in neighbourChunkOffsets)
                    {
                        if (Chunks.TryGetValue(chunkPosition + offset, out var neighbour))
                        {
                            // await neighbour.GenerateChunkAsync(Chunks);
                            neighbour.BuildChunk(Chunks);
                        }
                    }
                }
                catch
                {
                }
            }
        } 
        else if (RenderMode == RenderMode.RayTraced)
        {
            if (!_updated) 
            {
                Chunk chunk = Chunks[new Vector3Int(0, 0, 0)];
                List<Cube> cubes = new();
                for (int i = 0; i < Chunk.ChunkVolume; i++)
                {
                    if (chunk.voxels[i] == VoxelData.NameToVoxelId("air")) continue;
                
                    Vector3Int voxelPos = UnflattenIndex(i, Chunk.ChunkSize, Chunk.ChunkSize);
                
                    RTMaterial material = new RTMaterial(new(Random.Hash((uint)voxelPos.GetHashCode()), Random.Hash((uint)voxelPos.GetHashCode()),
                        Random.Hash((uint)voxelPos.GetHashCode())), new Vector3(0, 0, 0), 0f);
                
                    cubes.Add(new Cube(voxelPos, (int)chunk.voxels[i], material));
                }
            
                _rtVoxelStorageBuffer.SetData(Marshal.SizeOf<Cube>(), cubes.ToArray());
                _raytracingShader.SetUniform("NumCubes", cubes.Count);

                _updated = true;
            }
        }
        
        _chunkShader.Use();
        _chunkShader.SetUniform("viewPos", Player.Position, autoUse: false);
        _chunkShader.SetUniform("shadowsEnabled", Shadows ? 1 : 0, autoUse: false);
        _chunkShader.SetUniform("dirLight.direction", _lightDir, autoUse: false);
        _chunkShader.SetUniform("Wireframe", IsWireframe ? 1 : 0, autoUse: false);
        
        _skyboxShader.SetUniform("SunLightDirection", _lightDir);

        var result = Player.Traverse();
        AABB bounds = new AABB(result.Item1 ? result.Item2 : Vector3.Zero);
        box.Update(bounds);

        _test.Transform.Position = -_lightDir;
        _test.Transform.Position += Player.Position;
        
        ShadowMapper.UpdateMatrix(Player, _lightDir);

        Time.ElapsedTime += (float)args.Time;
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        
        _imGuiController.Update(this, (float)args.Time);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        TriangleCount = 0;
        VertexCount = 0;
        BatchCount = 0;
        LoadedChunks = 0;
        VisibleChunks = 0;
        
        // _testTexture.Use(TextureUnit.Texture5);

        if (RenderMode == RenderMode.Polygon)
        {
            // Render skybox with depth write disabled
            GL.DepthMask(false);
            _test.Render(Player);
            //_skyboxShader.Use();
            // _skybox.Render(Player);
            GL.DepthMask(true);

            if (Shadows)
            {
                ShadowMapper.Use();
                Render(mode: 0);
                ShadowMapper.Unuse(Size);
            }
            _tonemapper.Use();
            GL.PolygonMode(MaterialFace.FrontAndBack, IsWireframe ? PolygonMode.Line : PolygonMode.Fill);
            Render(mode: 1);
            box.Render(Player);
            _tonemapper.Unuse();
        
            GL.Disable(EnableCap.DepthTest);
            
            _tonemapper.Render(Player, ShadowMapper);
        } 
        else if (RenderMode == RenderMode.RayTraced)
        {
            TextureAtlas.AlbedoTexture.Use(TextureUnit.Texture5);
        
            _raytracingShader.Use();
            _raytracingShader.SetUniform("_TestTexture", 5, autoUse: false);
            _raytracingShader.SetUniform("Time", Time.ElapsedTime, autoUse: false);
        
            _rtVoxelStorageBuffer.Use();
        
            _raytracing.Render(Player, ShadowMapper);
        }
        
        GL.Enable(EnableCap.DepthTest);
        
        GL.Disable(EnableCap.DepthTest);
        UIRenderer.Render(Player);
        GL.Enable(EnableCap.DepthTest);
        
        if (ShowGui)
        {
            GL.Clear(ClearBufferMask.StencilBufferBit);
            GL.Disable(EnableCap.DepthTest); // disable depth testing for rendering ImGUI
        
            ImGui.DockSpaceOverViewport();
            
            //ImGui.ShowDemoWindow();
            OnImGuiRender();
        
            _imGuiController.Render();
        
            ImGuiController.CheckGLError("End of frame");
        
            GL.Enable(EnableCap.DepthTest);
        }

        if (Time.ElapsedTime >= _nextFpsUpdateTime)
        {
            _deltaTime = Time.DeltaTime;
            _nextFpsUpdateTime = (1f / FpsUpdateRate) + Time.ElapsedTime;
        }
        
        Vector3 pos = Vector3.Round(Player.Position, 2);
        string posX = pos.X.ToString("F2").PadRight(2);
        string posY = pos.Y.ToString("F2").PadRight(2);
        string posZ = pos.Z.ToString("F2").PadRight(2);
        
        string rendererString = RenderMode switch
        {
            RenderMode.Polygon => $"Standard Rendering (Vertices: {VertexCount:N0} | Triangles: {TriangleCount:N0} | Loaded Chunks: {LoadedChunks} Visible Chunks: {VisibleChunks} | Shadows Enabled: {Shadows})",
            RenderMode.RayTraced =>
                $"Ray Traced Rendering ({RayTracing.RaysPerPixel} SPP, {RayTracing.MaxLightBounces} Light Bounce(s))",
            _ => "Unknown Renderer"
        };
        
        Title = $"Voxel Game 0.0.0 (OpenGL 4 - {rendererString}) | Position: ({posX}, {posY}, {posZ}) | Frame Time: {Mathf.Round(_deltaTime * 1000f, 2)}ms ({Mathf.RoundToInt(1f / _deltaTime)} FPS)";
        
        SwapBuffers();
    }

    private void Render(int mode)
    {
        if (mode == 0) // 0 = shadow render pass
        {
            //_test.Render(Player);
            
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _lightBuffer);
            try
            {
                foreach (var chunk in Chunks)
                {
                    if (ShadowMapper.Frustum.IsBoundingBoxInFrustum(chunk.Value.Bounds) || !FrustumCulling)
                    {
                        var c = chunk.Value.Render(ShadowMapper.OrthographicMatrix, ShadowMapper.ViewMatrix, overrideShader: true, shaderOverride: _depthShader);
                    }
                }
            }
            catch (GLException e) // for some reason, I get an opengl invalid value error when rendering the chunks but
                                  // my error handler throws an exception when a gl exception is caught. this 'fixes'
                                  // the issue, it only stops the window closing but chunks all render correctly even
                                  // after the error is thrown
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(e.Message);
                Console.ResetColor();
            }
        }
        else
        {
            //_test.Render(Player);
            
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _lightBuffer);
            try
            {
                foreach (var chunk in Chunks)
                {
                    LoadedChunks++;

                    if (!chunk.Value.IsEmpty)
                    {
                        if (Player.Frustum.IsBoundingBoxInFrustum(chunk.Value.Bounds) || !FrustumCulling)
                        {
                            VisibleChunks += !chunk.Value.IsEmpty ? 1 : 0;
                    
                            var c = chunk.Value.Render(Player, ShadowMapper);
                            VertexCount   += c.vertexCount;
                            TriangleCount += c.triangleCount;
                        }
                    }
                }
            }
            catch (GLException e)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(e.Message);
                Console.ResetColor();
            }
        }
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        
        GL.Viewport(0, 0, e.Width, e.Height);
        Player.UpdateProjection(Size);
        
        _imGuiController.WindowResized(e.Width, e.Height);
        
        _tonemapper.UpdateSize(e.Size);
        _skybox.UpdateSize(e.Size);
        
        _raytracing.UpdateSize(e.Size);
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        
        _imGuiController.PressChar((char)e.Unicode);
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);
        
        if (!ShowGui)
            Player.Rotate(e.DeltaX, e.DeltaY);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        
        _imGuiController.MouseScroll(e.Offset);
    }
    
    private void OnImGuiRender()
    {
        ImGuiWindowFlags settingsFlags = 0;
        settingsFlags |= ImGuiWindowFlags.NoDocking;
        ImGui.SetNextWindowSize(new Vector2(1280/2f, 720/2f), ImGuiCond.FirstUseEver); 

        ImGui.GetStyle().Alpha = 0.7f;

        if (ImGui.Begin("Settings", settingsFlags))
        {
            ImGui.Checkbox("Fullscreen", ref IsFullscreen);
            ImGui.Checkbox("Wireframe Rendering", ref IsWireframe);

            ImGui.Checkbox("Shadows", ref Shadows);
            
            ImGui.Checkbox("Frustum Culling", ref FrustumCulling);
            
            ImGui.SliderInt("Render distance", ref Player.ChunkRenderDistance, 1, 32);
            ImGui.SliderInt("Max FPS", ref MaxFPS, 20, 480);

            ImGui.Combo("Chunk Builder Mode", ref ChunkBuilderMode, _imGuiChunkBuilderDropdown,
                _imGuiChunkBuilderDropdown.Length);
            
            if (ImGui.Button("Quit"))
                Close();
        }

        ImGui.End();
    }

    protected override void OnUnload()
    {
        base.OnUnload();
        
        GL.DeleteBuffer(_lightBuffer);
    }

    internal void RecalculateLightBuffer(int numLights)
    {
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _lightBuffer);
       
        GL.BufferData(BufferTarget.ShaderStorageBuffer, numLights * Marshal.SizeOf<PointLight>(), Lights.ToArray(), BufferUsageHint.DynamicDraw);
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, _lightBuffer);
        _chunkShader.SetUniform("NumPointLights", Lights.Count);
    }

    struct DirLight 
    {
        public Vector3 direction;

        public Vector3 ambient;
        public Vector3 diffuse;
        public Vector3 specular;

        public DirLight(Vector3 direction, Vector3 ambient, Vector3 diffuse, Vector3 specular)
        {
            this.direction = direction;
            this.ambient = ambient;
            this.diffuse = diffuse;
            this.specular = specular;
        }
    }
    
    [StructLayout(LayoutKind.Sequential)]
    internal struct PointLight 
    {
        public Vector3 position;
        public float _padding1;
        public float constant;
        public float linear;
        public float quadratic;
        public float _padding2;

        public Vector3 ambient;
        public float _padding3;
        public Vector3 diffuse;
        public float _padding4;
        public Vector3 specular;
        public float _padding5;

        public PointLight(Vector3 position, float range, float intensity, Vector3 ambient, Vector3 diffuse,
            Vector3 specular)
        {
            this.position = position;
            this.ambient = ambient * intensity;
            this.diffuse = diffuse * intensity;
            this.specular = specular * intensity;

            constant = 1.0f;
            linear = 4.5f / range;
            quadratic = 75.0f / (range * range);

            Lights.Add(this);

            _padding1 = 1;
            _padding2 = 2;
            _padding3 = 3;
            _padding4 = 4;
            _padding5 = 4;
        }
    }
    
    [StructLayout(LayoutKind.Sequential)]
    internal struct Cube
    {
        public Vector3 min;
        private float _padding0;
        
        public Vector3 max;
        public int id;
        
        public RTMaterial material;

        public Cube(Vector3 min, Vector3 max, int id, RTMaterial material)
        {
            this.min = min;
            this.max = max;
            this.id = id;
            this.material = material;

            _padding0 = 0;
        }
        
        public Cube(Vector3 offset, int id, RTMaterial material)
        {
            min = new Vector3(-0.5f, -0.5f, -0.5f) + offset;
            max = new Vector3(0.5f, 0.5f, 0.5f) + offset;
            this.id = id;
            this.material = material;

            _padding0 = 0;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RTMaterial
    {
        public Vector3 colour;
        public float emissionStrength;

        public Vector3 emissionColour;

        private float _padding0;

        public RTMaterial(Vector3 colour, Vector3 emissionColour, float emissionStrength)
        {
            this.colour = colour;
            this.emissionColour = emissionColour;
            this.emissionStrength = emissionStrength;

            _padding0 = 0;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ShaderVoxelData
    {
        public uint id;
        public int[] texFaces;

        public ShaderVoxelData(uint id, int[] texFaces)
        {
            this.id = id;
            this.texFaces = texFaces;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RTChunk
    {
        public Vector3 min; private float _padding0;
        public Vector3 max; private float _padding1;

        public int voxelStartIndex;
        public int voxelCount;

        private int _padding2;
        private int _padding3;

        public RTChunk(Vector3 min, Vector3 max, int voxelStartIndex, int voxelCount)
        {
            this.min = min;
            this.max = max;
            this.voxelStartIndex = voxelStartIndex;
            this.voxelCount = voxelCount;

            _padding0 = 0;
            _padding1 = 0;
            _padding2 = 0;
            _padding3 = 0;
        }
    }

    public static (uint? voxelId, int voxelIndex, Vector3Int voxelLocalPos, Vector3Int chunk) GetVoxelAtPosition(Vector3Int position)
    {
        // Determine the chunk position by dividing the voxel position by the chunk size
        Vector3Int chunkPosition = new Vector3Int(
            Mathf.FloorToInt((float)position.X / Chunk.ChunkSize),
            Mathf.FloorToInt((float)position.Y / Chunk.ChunkSize),
            Mathf.FloorToInt((float)position.Z / Chunk.ChunkSize)
        );

        // Check if the chunk exists in the dictionary
        if (Chunks.TryGetValue(chunkPosition, out Chunk chunk))
        {
            // Calculate the local voxel position within the chunk
            Vector3Int localPosition = new Vector3Int(
                position.X % Chunk.ChunkSize,
                position.Y % Chunk.ChunkSize,
                position.Z % Chunk.ChunkSize
            );

            // Ensure positive local positions by adjusting with Chunk.ChunkSize
            localPosition = (localPosition + Chunk.ChunkSize) % Chunk.ChunkSize;

            // Flatten the local position to get the voxel index in the chunk's voxel array
            int index = Chunk.FlattenIndex3D(localPosition.X, localPosition.Y, localPosition.Z, Chunk.ChunkSize, Chunk.ChunkSize);

            // Return the voxel value at the calculated index
            return (chunk.voxels[index], index, localPosition, chunkPosition);
        }

        // Return null if the chunk does not exist
        return (null, 0, Vector3Int.Zero, Vector3Int.Zero);
    }

    public static Vector3Int UnflattenIndex(int index, int width, int height)
    {
        int z = index / (width * height);
        int y = (index / width) % height;
        int x = index % width;
        return new(x, y, z);
    }
    
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int MessageBox(IntPtr hWnd, String text, String caption, uint type);
    
    // Constants for the MessageBox type
    private const uint MB_OK = 0x00000000;
    private const uint MB_ICONERROR = 0x00000010;

    internal void ShowErrorMessage(Exception ex, string title, bool showStackTrace = true)
    {
        CursorState = CursorState.Normal;
        MessageBox(IntPtr.Zero, showStackTrace ? $"{ex.Message}\nStack Trace:\n{ex.StackTrace}" : $"{ex.Message}",
            title, MB_OK | MB_ICONERROR);
    }

    public static void RegisterMainThreadAction(Action action)
    {
        _mainThreadActions.Enqueue(action);
    }
}
