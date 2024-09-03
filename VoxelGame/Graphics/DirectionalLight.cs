using OpenTK.Mathematics;
using VoxelGame.Maths;
using Vector3 = VoxelGame.Maths.Vector3;

namespace VoxelGame.Graphics;

public class DirectionalLight
{
    public Vector3 LightPosition { get; set; }
    public Vector3 LightDirection => LightPosition.Normalized;

    // TODO: csm
    public bool Shadows { get; set; } = true;
    public ShadowMapQuality ShadowQuality { get; private set; } = ShadowMapQuality.Ultra;
    public bool SoftShadows { get; set; } = true;

    public int OrthoSize { get; set; } = 50;
    
    public Matrix4 LightProjMatrix { get; private set; }
    public Matrix4 LightViewMatrix { get; private set; }

    internal int DepthTexture => _shadowDepthTexture;
    
    private int _shadowFbo;
    private int _shadowDepthTexture;

    public DirectionalLight()
    {
        // Generate the framebuffer for shadowmapping
        // All of this is generated regardless of the Shadows state
        //
        // All that affects is whether the shadow render pass is ran
        //    and whether the deferred renderer samples a shadow map
        
        GenerateShadowMap();
    }

    private void GenerateShadowMap()
    {
        int quality = (int)ShadowQuality;
        
        _shadowFbo = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _shadowFbo);
        
        _shadowDepthTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _shadowDepthTexture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent, quality, quality, 0,
            PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
        float[] borderCol = [ 1.0f, 1.0f, 1.0f, 1.0f ];
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, borderCol);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
            TextureTarget.Texture2D, _shadowDepthTexture, 0);
        
        // Check framebuffer completeness
        var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != FramebufferErrorCode.FramebufferComplete)
            throw new Exception($"Framebuffer is not complete: {status}");
        
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    internal void UpdateMatrix(Vector3 cameraPosition)
    {
        LightProjMatrix = Matrix4.CreateOrthographicOffCenter(-OrthoSize, OrthoSize, -OrthoSize, OrthoSize, 1f, 500f);

        Vector3 lightDir = LightPosition.Normalized;

        // The distance from the camera that the sun is considered to be
        const float SunViewLength = 100f;
        Vector3 eyePosition = lightDir * SunViewLength + cameraPosition;
        LightViewMatrix = Matrix4.LookAt(eyePosition, cameraPosition, Vector3.Up);
    }

    internal void Use()
    {
        int quality = (int)ShadowQuality;
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _shadowFbo);
        GL.Viewport(0, 0, quality, quality);
        GL.CullFace(CullFaceMode.Front);
        GL.Clear(ClearBufferMask.DepthBufferBit);
    }

    internal void Unuse(Vector2Int vpSize)
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Viewport(0, 0, vpSize.X, vpSize.Y);
        GL.CullFace(CullFaceMode.Back);
    }

    public void ChangeShadowQuality(ShadowMapQuality shadowQuality)
    {
        ShadowQuality = shadowQuality;
        GenerateShadowMap();
    }
}