// 4,165 lines of code :D

using System.Runtime.InteropServices;
using ImGuiNET;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using VoxelGame.Maths;
using VoxelGame.Rendering;
using Vector3 = VoxelGame.Maths.Vector3;

namespace VoxelGame;

public sealed class Engine : GameWindow
{
    public static readonly string DataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VoxelGame");
    
    public bool ShowGui { get; set; }

    public bool IsFullscreen;
    public bool IsWireframe;
    public bool Shadows = true;

    public const bool EnableFrustumCulling = false;

    public readonly Player Player;

    internal static List<PointLight> Lights = new();

    internal static int TriangleCount;
    internal static int VertexCount;
    internal static int LoadedChunks;
    internal static int VisibleChunks;

    internal ImGuiController _imGuiController;

    internal readonly ShadowMapper ShadowMapper;
    private Shader _depthShader;
    
    private Shader _chunkShader;
    internal static Dictionary<Vector3Int, Chunk> Chunks;
    private Queue<Vector3Int> _chunksToBuild;
    private const int MaxChunksToBuildPerFrame = 32;

    private Shader _meshShader;

    private Skybox _skybox;
    private Shader _skyboxSkyShader;
    private Shader _skyboxVoidShader;

    private List<AABB> collisions = new();

    private Mesh _test;
    
    // private readonly Vector3 _lightColour = new Vector3(1.0f, 0.898f, 0.7f);
    private readonly Vector3 _lightColour = new Vector3(1.0f, 1.0f, 1.0f);

    private bool _newLightThisFrame = true; // this is true when a (or multiple) new light is added to the scene. The light buffer is recalculated when this is true

    private int _lightBuffer;

    // private Vector3 _lightDir = new Vector3(-2, -1, -0.5f);
    private Vector3 _lightDir = new Vector3(-0.5f, -1f, -0.5f);

    private string[] _imGuiChunkBuilderDropdown =
    [
        "Not Blocking",
        "Fully blocking"
    ];

    public static int ChunkBuilderMode = 0;

    private const float FpsUpdateRate = 3f;
    private float _nextFpsUpdateTime = 0;
    private float _fps;

    public Engine(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws)
    {
        ShadowMapper = new();
        CenterWindow();

        Player = new Player(Size);
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        // GL setup
        GL.Enable(EnableCap.Multisample);
        GL.Enable(EnableCap.CullFace);
        GL.Enable(EnableCap.DepthTest);

        GL.CullFace(CullFaceMode.Back);
        GL.FrontFace(FrontFaceDirection.Cw);

        GL.ClearColor(0.6f, 0.75f, 1f, 1f);
        // GL.ClearColor(Colour.Black);
        
        // Initialise ImGUI
        _imGuiController = new ImGuiController(Size.X, Size.Y);
        // load any imgui fonts here

        TextureAtlas.Init();
        // Physics.Init();

        // Make the window visible after setting up so it appears in place and not in a random location
        IsVisible = true;

        _depthShader = Shader.Load("Assets/Shaders/depth.vert", "Assets/Shaders/depth.frag");
        _chunkShader = Shader.Load("Assets/Shaders/chunk-shader.vert", "Assets/Shaders/chunk-shader.frag");

        _chunkShader.SetUniform("material.diffuse", 0);
        _chunkShader.SetUniform("material.specular", 1);
        _chunkShader.SetUniform("material.shininess", 128.0f);

        Vector3 ambientLighting = Vector3.One * 0.08f;
        DirLight dirLight = new(_lightDir, ambientLighting, Vector3.One * 1f, _lightColour);
        _chunkShader.SetUniform("dirLight.direction", dirLight.direction);
        _chunkShader.SetUniform("dirLight.ambient"  , dirLight.ambient);
        _chunkShader.SetUniform("dirLight.diffuse"  , dirLight.diffuse);
        _chunkShader.SetUniform("dirLight.specular" , dirLight.specular);
        
        _chunkShader.SetUniform("fogColour", new Vector3(0.6f, 0.75f, 1f));
        _chunkShader.SetUniform("fogDensity", 0.03f);

        _skyboxSkyShader = Shader.Load("Assets/Shaders/skybox.vert", "Assets/Shaders/skybox.frag");
        _skyboxSkyShader.SetUniform("fogColour", new Vector3(0.6f, 0.75f, 1f));
        _skyboxSkyShader.SetUniform("fogDensity", 0.07f);
        _skyboxVoidShader = Shader.Load("Assets/Shaders/skybox.vert", "Assets/Shaders/skybox.frag");
        _skyboxVoidShader.SetUniform("fogColour", new Vector3(0.6f, 0.75f, 1f));
        _skyboxVoidShader.SetUniform("fogDensity", 0.03f);
        // _skyboxShader = Shader.StandardShader;
        _skybox = new Skybox(_skyboxSkyShader, _skyboxVoidShader);
        
        Chunks = new Dictionary<Vector3Int, Chunk>();
        _chunksToBuild = new Queue<Vector3Int>();

        _lightBuffer = GL.GenBuffer();

        _test = MeshUtility.GenerateCube(Shader.StandardShader);
        _test.Transform.Scale = Vector3.One * 0.05f;
        for (int i = 0; i < _test.Colours.Length; i++)
        {
            _test.Colours[i] = Colour.Yellow;
        }

        // Lights.Add(new PointLight(Player.Position, 50f, ambientLighting, new(1.0f, 1.0f, 1.0f), new Vector3(1.0f, 1.0f, 1.0f)));
        // RecalculateLightBuffer(1);
        // _shader.SetUniform("NumPointLights", 1);

        // UIRenderer.CreateQuad(Vector2.Zero, Vector3.One);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        Input._keyboardState = KeyboardState;
        Time.DeltaTime = (float)args.Time;
        
        Vector3Int playerPosition = Vector3.Round(Player.Position / Chunk.ChunkSize);
        int renderDistance = Player.ChunkRenderDistance;

        List<Vector3Int> chunksToRemove = new List<Vector3Int>();

        foreach (var chunk in Chunks)
        {
            var chunkPos = chunk.Key;
            int dx = chunkPos.X - playerPosition.X;
            int dy = chunkPos.Y - playerPosition.Y;
            int dz = chunkPos.Z - playerPosition.Z;

            if (Math.Abs(dx) > renderDistance || Math.Abs(dy) > renderDistance || Math.Abs(dz) > renderDistance)
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

        // foreach (var chunk in Chunks.Values)
        // {
        //     collisions.AddRange(chunk.GenerateCollisions());
        // }
        Player.Update(Size);
        // Physics.ResolveCollisions(Player, collisions.ToArray());
        
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
            var chunk = Chunks[Vector3Int.Zero];
            for (int i = 0; i < chunk.voxels.Length; i++)
            {
                chunk.voxels[i] = chunk.voxels[i] == 4u ? 4u : 0u;
            }
            chunk.RebuildChunk(Chunks, recursive: true);
        }

        if (Input.GetKeyDown(Keys.Enter))
        {
            Lights.Add(new PointLight(Player.Position, 50f, Vector3.Zero, _lightColour, _lightColour));
            RecalculateLightBuffer(Lights.Count);
        }

        for (int x = -Player.ChunkRenderDistance; x < Player.ChunkRenderDistance; x++)
        {
            for (int y = -Player.ChunkRenderDistance; y < Player.ChunkRenderDistance; y++)
            {
                for (int z = -Player.ChunkRenderDistance; z < Player.ChunkRenderDistance; z++)
                {
                    Vector3Int chunkPosition = new Vector3Int(x, y, z) + playerPosition;
                    Vector3Int chunkWorldPosition = chunkPosition * Chunk.ChunkSize;

                    if (!Chunks.ContainsKey(chunkPosition) && chunkWorldPosition.Y >= 0)
                    {
                        // TODO: Load chunks from disk
                        var newChunk = new Chunk(chunkWorldPosition, _chunkShader);
                        _chunksToBuild.Enqueue(chunkPosition); // Queue the chunk for building
                        Chunks[chunkPosition] = newChunk; // Add the chunk to the dictionary
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
                var chunk = Chunks[chunkPosition];
                chunk.RebuildChunk(Chunks, recursive: true);

                // Force neighbouring chunks to rebuild
                Vector3Int[] neighbourChunkOffsets =
                {
                    new Vector3Int(0, 0, 1),
                    new Vector3Int(0, 0, -1),
                    new Vector3Int(0, 1, 0),
                    new Vector3Int(0, -1, 0),
                    new Vector3Int(1, 0, 0),
                    new Vector3Int(-1, 0, 0),
                };

                foreach (var offset in neighbourChunkOffsets)
                {
                    if (Chunks.TryGetValue(chunkPosition + offset, out var neighbour))
                    {
                        neighbour.RebuildChunk(Chunks);
                    }
                }
            }
            catch
            {
            }
        }

        // _skybox.Transform.Position = Player.Position;
        // _skybox.Transform.Rotation += Vector3.Forward * (2f * Time.DeltaTime);

        const float sunSpeed = 1f;
        // update light direction to rotate in a circle
        //_lightDir = Vector3.RotateX(_lightDir, sunSpeed * Time.DeltaTime);
        
        _chunkShader.SetUniform("viewPos", Player.Position);

        // Player.Yaw = 0;
        // Player.Pitch = 0;

        _test.Transform.Position = -_lightDir;
        _test.Transform.Position += Player.Position;
        // _test.Transform.LookAt(Player.Position);
        
        //_shader.SetUniform("dirLight.direction", _lightDir);
        
        _skyboxSkyShader.SetUniform("viewPos", Player.Position);
        
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
        LoadedChunks = 0;
        VisibleChunks = 0;
        
        // Render skybox with depth write disabled
        GL.DepthMask(false);
        _test.Render(Player);
        //_skyboxShader.Use();
        // _skybox.Render(Player);
        GL.DepthMask(true);
        
        ShadowMapper.Use();
        Render(mode: 0);
        ShadowMapper.Unuse(Size);
        Render(mode: 1);
        
        if (ShowGui)
        {
            GL.Clear(ClearBufferMask.StencilBufferBit);
            GL.Disable(EnableCap.DepthTest); // disable depth testing for rendering ImGUI
        
            ImGui.DockSpaceOverViewport();
            
            ImGui.ShowDemoWindow();
            //OnImGuiRender();
        
            _imGuiController.Render();
        
            ImGuiController.CheckGLError("End of frame");
        
            GL.Enable(EnableCap.DepthTest);
        }

        if (Time.ElapsedTime >= _nextFpsUpdateTime)
        {
            _fps = Time.Fps;
            _nextFpsUpdateTime = (1f / FpsUpdateRate) + Time.ElapsedTime;
        }
        
        Title = $"Position: {Vector3.Round(Player.Position, 2)} | Vertices: {VertexCount:N0} Triangles: {TriangleCount:N0} | Loaded Chunks: {LoadedChunks} Visible Chunks: {VisibleChunks} | FPS: {_fps}";
        
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
                    LoadedChunks++;
                    VisibleChunks++;
                    
                    var c = chunk.Value.Render(ShadowMapper.OrthographicMatrix, ShadowMapper.ViewMatrix, overrideShader: true, shaderOverride: _depthShader);
                    VertexCount   += c.vertexCount;
                    TriangleCount += c.triangleCount;
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
                        if (Player.Frustum.IsInFrustum(chunk.Value) || !EnableFrustumCulling)
                        {
                            VisibleChunks++;
                    
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
            
            ImGui.SliderInt("Render distance", ref Player.ChunkRenderDistance, 1, 32);

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

        public PointLight(Vector3 position, float range, Vector3 ambient, Vector3 diffuse, Vector3 specular)
        {
            this.position = position;
            this.ambient = ambient;
            this.diffuse = diffuse;
            this.specular = specular;
            
            constant = 1.0f;
            linear = 4.5f / range;
            quadratic = 75.0f / (range * range);
            
            Lights.Add(this);

            _padding1 = 1;
            _padding1 = 2;
            _padding2 = 3;
            _padding3 = 4;
            _padding4 = 4;
            _padding5 = 4;
        }
    }

    public static uint? GetVoxelAtPosition(Vector3Int position)
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
            return chunk.voxels[index];
        }

        // Return null if the chunk does not exist
        return null;
    }
}
