using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using VoxelGame.Maths;
using VoxelGame.Rendering;
using ErrorCode = OpenTK.Graphics.OpenGL4.ErrorCode;

namespace VoxelGame;

public sealed class Engine : GameWindow
{
    public static Camera Camera { get; private set; }

    private Shader RTShader;
    private SSEffect RayTracer;
    private ShaderStorageBuffer ChunkBuffer;
    private ShaderStorageBuffer VoxelBuffer;
    
    private Texture2D _texture;

    private int _fpsTotal;
    
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
        
        _texture = new Texture2D("assets/textures/uv-checker.png", useLinearSampling: false);
        
        // Initialise the ray tracer
        RTShader = Shader.Load("assets/shaders/raytracer.vert", "assets/shaders/raytracer.frag");
        RayTracer = new SSEffect(RTShader, Size, false);
        
        // Create the chunk buffer
        ChunkBuffer = new ShaderStorageBuffer(0);
        
        // Create the voxel buffer
        VoxelBuffer = new ShaderStorageBuffer(1);
        
        RTMaterial mat = new RTMaterial(new Vector3(1, 0, 0), new Vector3(0, 0, 0), 0);

        List<Chunk> chunks = new List<Chunk>
        {
            new Chunk(new Vector3Int(0, 0, 0), null),
            new Chunk(new Vector3Int(1, 0, 0), null),
            new Chunk(new Vector3Int(-1, 0, 0), null),
            new Chunk(new Vector3Int(0, 1, 0), null),
            new Chunk(new Vector3Int(0, -1, 0), null),
            new Chunk(new Vector3Int(0, 0, 1), null),
            new Chunk(new Vector3Int(0, 0, -1), null),
        };

        List<RTCube> cubes = new();

        List<RTChunk> rtChunks = new List<RTChunk>();
        int voxelStartIndex = 0;

        foreach (Chunk chunk in chunks)
        {
            cubes.AddRange(chunk.GenCubes(mat));

            rtChunks.Add(new RTChunk(chunk.Bounds.Min, chunk.Bounds.Max, chunk.SolidVoxelCount, voxelStartIndex));
            voxelStartIndex += chunk.SolidVoxelCount;
        }

        RTChunk[] chunkArray = rtChunks.ToArray();
        
        ChunkBuffer.SetData(Unsafe.SizeOf<RTChunk>(), chunkArray);
        
        VoxelBuffer.SetData(Unsafe.SizeOf<RTCube>(), cubes.ToArray());
        
        // Set RT shader parameters
        RTShader.Use();
        
        RTShader.SetVector3("SkyColourZenith", new Vector3(0.5019608f, 0.67058825f, 0.8980393f), autoUse: false);
        RTShader.SetVector3("SkyColourHorizon", new Vector3(1, 1, 1), autoUse: false);
        RTShader.SetVector3("GroundColour", new Vector3(0.5647059f, 0.5254902f, 0.5647059f), autoUse: false);
        // RTShader.SetVector3("GroundColour", new Vector3(0.08f, 0.08f, 0.08f), autoUse: false);
        
        RTShader.SetInt("NumChunks", chunkArray.Length);
        
        RTShader.SetInt("TestTexture", 5);
        
        RTShader.SetInt("MaxLightBounces", 3);
        RTShader.SetInt("RaysPerPixel", 5);
        RTShader.SetFloat("SkyboxIntensity", 1f);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        
        // Update loop

        Time.DeltaTime = (float)args.Time;
        Time.NumFrames++;
        Input._kbState = KeyboardState;

        // Close the window when the escape key is pressed
        if (Input.GetKeyDown(Keys.Escape))
            Close();
        
        // Update the camera
        Camera.Update();

        Title = $"FPS Average (Actual): {Time.AvgFps} ({Time.Fps})";

        Time.ElapsedTime += Time.DeltaTime;
        Time.UpdateFps();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        
        // Render loop
        
        // Clear the colour and depth buffers
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        // Render here
        
        _texture.Use(TextureUnit.Texture5);
        
        // ChunkBuffer.Use();
        // VoxelBuffer.Use();
        
        RayTracer.Render(Camera);

        var err = GL.GetError();
        if (err != ErrorCode.NoError)
            throw new Exception(err.ToString());
        
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
        
        // Resize the ray tracer quad
        RayTracer.UpdateSize(e.Size);
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);
        
        // Update the camera's rotation with the mouse delta
        Camera.Rotate(e.Delta);
    }
    
    [StructLayout(LayoutKind.Sequential)]
    internal struct RTCube
    {
        public Vector3 min;
        private float _padding0;
        
        public Vector3 max;
        public int id;
        
        public RTMaterial material;

        public RTCube(Vector3 min, Vector3 max, int id, RTMaterial material)
        {
            this.min = min;
            this.max = max;
            this.id = id;
            this.material = material;

            _padding0 = 0;
        }
        
        public RTCube(Vector3 offset, int id, RTMaterial material)
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
    internal struct RTChunk 
    {
        public Vector3 boundsMin; public int voxelCount;
        public Vector3 boundsMax; public int voxelStartIndex;

        public RTChunk(Vector3 boundsMin, Vector3 boundsMax, int voxelCount, int voxelStartIndex)
        {
            this.boundsMin = boundsMin;
            this.boundsMax = boundsMax;
            this.voxelCount = voxelCount;
            this.voxelStartIndex = voxelStartIndex;

            // _padding0 = 0;
            // _padding1 = 0;
        }
    }
}
