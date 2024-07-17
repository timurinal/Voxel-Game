using OpenTK.Mathematics;
using VoxelGame.Maths;
using Vector3 = VoxelGame.Maths.Vector3;

namespace VoxelGame.Rendering;

internal class ShadowMapper
{
    public int DepthMap => _depthMap;
    
    public Matrix4 OrthographicMatrix;
    public Matrix4 ViewMatrix;

    public Matrix4 VPMatrix;

    public const float SunViewDistance = 1000f;

    public const float NearPlane = 1.0f, FarPlane = 5000f;

    public const int ShadowMapWidth = 8192, ShadowMapHeight = ShadowMapWidth;

    public const float OrthographicSize = 200f;

    public Frustum Frustum;

    private int _depthMapFbo, _depthMap;

    public ShadowMapper()
    {
        _depthMapFbo = GL.GenFramebuffer();
        _depthMap = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _depthMap);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent, 
            ShadowMapWidth, ShadowMapHeight, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
        float[] borderCol = [ 1.0f, 1.0f, 1.0f, 1.0f ];
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, borderCol);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _depthMapFbo);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, _depthMap, 0);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        Frustum = new Frustum();
    }

    internal void Use()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _depthMapFbo);
        GL.Viewport(0, 0, ShadowMapWidth, ShadowMapHeight);
        GL.CullFace(CullFaceMode.Front);
        GL.Clear(ClearBufferMask.DepthBufferBit);
    }

    internal void Unuse(Vector2Int size)
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Viewport(0, 0, size.X, size.Y);
        GL.CullFace(CullFaceMode.Back);
    }

    internal void UpdateMatrix(Player player, Vector3 lightDir)
    {
        OrthographicMatrix = Matrix4.CreateOrthographicOffCenter(-OrthographicSize, OrthographicSize, -OrthographicSize, OrthographicSize, NearPlane, FarPlane);
        Vector3 eyePosition = -lightDir * SunViewDistance + player.Position;
        ViewMatrix = Matrix4.LookAt(eyePosition, player.Position, Vector3.Up);

        VPMatrix = ViewMatrix * OrthographicMatrix;
        
        Frustum.CalculateFrustum(VPMatrix);
    }
}