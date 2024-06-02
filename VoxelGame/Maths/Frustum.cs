using VoxelGame.Rendering;
using VoxelGame.Maths;
using OpenTK.Mathematics;
using Vector3 = VoxelGame.Maths.Vector3;

public class Frustum
{
    private Player player;
    private float factorY;
    private float tanY;
    private float factorX;
    private float tanX;

    public Frustum(Player player)
    {
        this.player = player;

        float halfY = player.VFov * 0.5f;
        factorY = 1.0f / MathF.Cos(halfY);
        tanY = MathF.Tan(halfY);

        float halfX = player.HFov * 0.5f;
        factorX = 1.0f / MathF.Cos(halfX);
        tanX = MathF.Tan(halfX);
    }
    
    public bool IsInFrustum(Chunk chunk)
    {
        // vector to sphere center
        Vector3 sphereVec = chunk.Center - player.Position;

        // outside the NEAR and FAR planes?
        float sz = Vector3.Dot(sphereVec, player.Forward);
        if (!(player.NearClipPlane - Chunk.ChunkSphereRadius <= sz && sz <= player.FarClipPlane + Chunk.ChunkSphereRadius))
        {
            return false;
        }

        // outside the TOP and BOTTOM planes?
        float sy = Vector3.Dot(sphereVec, player.Up);
        float dstY = factorY * Chunk.ChunkSphereRadius + sz * tanY;
        if (!(-dstY <= sy && sy <= dstY))
        {
            return false;
        }

        // outside the LEFT and RIGHT planes?
        float sx = Vector3.Dot(sphereVec, player.Right);
        float dstX = factorX * Chunk.ChunkSphereRadius + sz * tanX;
        if (!(-dstX <= sx && sx <= dstX))
        {
            return false;
        }

        return true;
    }
}