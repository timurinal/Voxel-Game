﻿// 3,925 lines of code :D

using System.Diagnostics;
using System.Runtime.CompilerServices;
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
using ErrorCode = OpenTK.Graphics.OpenGL4.ErrorCode;
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

    internal static List<PointLight> Lights = new();

    internal static int TriangleCount;
    internal static int VertexCount;
    internal static int LoadedChunks;
    internal static int VisibleChunks;

    internal readonly ShadowMapper ShadowMapper;
    private Shader _depthShader;
    
    private Shader _chunkShader;
    private Dictionary<Vector3Int, Chunk> _chunks;
    private Queue<Vector3Int> _chunksToBuild;
    private const int MaxChunksToBuildPerFrame = 32;

    private Shader _meshShader;

    private Skybox _skybox;
    private Shader _skyboxSkyShader;
    private Shader _skyboxVoidShader;
    
    private readonly Vector3 _lightColour = new Vector3(1.0f, 0.898f, 0.7f);

    private bool _newLightThisFrame = true; // this is true when a (or multiple) new light is added to the scene. The light buffer is recalculated when this is true

    private int _lightBuffer;

    private Vector3 _lightDir = new Vector3(-2, -1, -0.5f);

    public Engine(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws)
    {
        ShadowMapper = new();
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
        // GL.ClearColor(Colour.Black);

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
        
        _chunks = new Dictionary<Vector3Int, Chunk>();
        _chunksToBuild = new Queue<Vector3Int>();

        _lightBuffer = GL.GenBuffer();
        
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

        foreach (var chunk in _chunks)
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
            _chunks.Remove(chunkPos);
        }
        
        // Physics.Update();
        
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
            var chunk = _chunks[Vector3Int.Zero];
            for (int i = 0; i < chunk.voxels.Length; i++)
            {
                chunk.voxels[i] = chunk.voxels[i] == 4u ? 4u : 0u;
            }
            chunk.RebuildChunk(_chunks, recursive: true);
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

                    if (!_chunks.ContainsKey(chunkPosition) && chunkWorldPosition.Y >= 0)
                    {
                        // TODO: Load chunks from disk
                        var newChunk = new Chunk(chunkWorldPosition, _chunkShader);
                        _chunksToBuild.Enqueue(chunkPosition); // Queue the chunk for building
                        _chunks[chunkPosition] = newChunk; // Add the chunk to the dictionary
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

        // _skybox.Transform.Position = Player.Position;
        // _skybox.Transform.Rotation += Vector3.Forward * (2f * Time.DeltaTime);

        const float sunSpeed = 1f;
        // update light direction to rotate in a circle
        //_lightDir = Vector3.RotateX(_lightDir, sunSpeed * Time.DeltaTime);
        
        _chunkShader.SetUniform("viewPos", Player.Position);
        
        //_shader.SetUniform("dirLight.direction", _lightDir);
        
        _skyboxSkyShader.SetUniform("viewPos", Player.Position);
        
        ShadowMapper.UpdateMatrix(Player, _lightDir);

        Time.ElapsedTime += (float)args.Time;
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        TriangleCount = 0;
        VertexCount = 0;
        LoadedChunks = 0;
        VisibleChunks = 0;
        
        // Render skybox with depth write disabled
        GL.DepthMask(false);
        //_skyboxShader.Use();
        // _skybox.Render(Player);
        GL.DepthMask(true);
        
        ShadowMapper.Use();
        Render(mode: 0);
        ShadowMapper.Unuse(Size);
        Render(mode: 1);
        
        GL.Disable(EnableCap.DepthTest);
        UIRenderer.Render(Player);
        GL.Enable(EnableCap.DepthTest);

        Title = $"Vertices: {VertexCount:N0} Triangles: {TriangleCount:N0} | Loaded Chunks: {LoadedChunks} Visible Chunks: {VisibleChunks} | FPS: {Time.Fps}";
        
        SwapBuffers();
    }

    private void Render(int mode)
    {
        if (mode == 0) // 0 = shadow render pass
        {
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _lightBuffer);
            try
            {
                foreach (var chunk in _chunks)
                {
                    LoadedChunks++;
                    VisibleChunks++;
                    
                    var c = chunk.Value.Render(ShadowMapper.OrthographicMatrix, ShadowMapper.ViewMatrix, overrideShader: true, shaderOverride: _depthShader);
                    VertexCount   += c.vertexCount;
                    TriangleCount += c.triangleCount;
                }
            }
            catch (GLException e) // for some reason, I get an opengl invalid value error when rendering the chunks but my error handler throws an exception when a gl exception is caught. this 'fixes' the issue, it only stops the window closing but chunks all render correctly even after the error is thrown
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(e.Message);
                Console.ResetColor();
            }
        }
        else
        {
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _lightBuffer);
            try
            {
                foreach (var chunk in _chunks)
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
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);
        
        Player.Rotate(e.DeltaX, e.DeltaY);
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
}
