using OpenTK.Mathematics;
using VoxelGame.Maths;
using Vector3 = VoxelGame.Maths.Vector3;

namespace VoxelGame.Rendering;

internal class ShadowMapper
{
    public Matrix4 OrthographicMatrix;
    public Matrix4 ViewMatrix;

    public const float SunViewDistance = 50f;

    public const float NearPlane = 1.0f, FarPlane = 500f;

    public const int ShadowMapWidth = 1024, ShadowMapHeight = 1024;

    private int _depthMapFbo, _depthMap;

    internal ShadowMapper()
    {
        _depthMapFbo = GL.GenFramebuffer();
        _depthMap = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _depthMap);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent, 
            ShadowMapWidth, ShadowMapHeight, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _depthMapFbo);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, _depthMap, 0);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    internal void Use()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _depthMapFbo);
        GL.Viewport(0, 0, ShadowMapWidth, ShadowMapHeight);
        GL.Clear(ClearBufferMask.DepthBufferBit);
    }

    internal void Unuse(Vector2Int size)
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Viewport(0, 0, size.X, size.Y);
    }

    internal void UpdateMatrix(Player player, Vector3 lightDir)
    {
        OrthographicMatrix = Matrix4.CreateOrthographicOffCenter(-10f, 10f, -10f, 10f, NearPlane, FarPlane);
        Vector3 eyePosition = -lightDir * SunViewDistance + player.Position;
        ViewMatrix = Matrix4.LookAt(eyePosition, player.Position, Vector3.Up);
    }
}