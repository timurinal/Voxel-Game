﻿using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using VoxelGame.Maths;
using VoxelGame.Rendering;

namespace VoxelGame;

public sealed class Engine : GameWindow
{
    public static Camera Camera { get; private set; }

    private Shader _shader;
    private Material _material;
    private Texture2D _texture;
    private Mesh _cube;
    
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
        
        // Load a shader/texture and assign it to a material
        _shader = Shader.Load("assets/shaders/chunk-shader.vert", "assets/shaders/chunk-shader.frag");
        
        _shader.SetInt("TestTexture", 0);
        
        _texture = new Texture2D("assets/textures/uv-checker.png");
        _material = new Material(_shader)
        {
            surfaceMode = Material.SurfaceMode.Opaque
        };

        // Create a cube with the new material
        _cube = MeshUtility.CreateCube(_material);
        _cube.Transform.Position = new Vector3(5, 0, 0);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        
        // Update loop

        Time.DeltaTime = (float)args.Time;
        Input._kbState = KeyboardState;

        // Close the window when the escape key is pressed
        if (Input.GetKeyDown(Keys.Escape))
            Close();
        
        // Update the camera
        Camera.Update();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        
        // Render loop
        
        // Clear the colour and depth buffers
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        // Render here
        
        _texture.Use(TextureUnit.Texture0);
        _cube.Render(Camera);
        
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
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);
        
        // Update the camera's rotation with the mouse delta
        Camera.Rotate(e.Delta);
    }
}
