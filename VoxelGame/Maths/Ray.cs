using VoxelGame.Rendering;

namespace VoxelGame.Maths;

public struct Ray
{
    public Vector3 Origin;
    public Vector3 Direction;

    public Ray(Vector3 origin, Vector3 direction)
    {
        Origin = origin;
        Direction = Vector3.Normalize(direction);
    }

    public static Ray GetCameraRay(Player player)
    {
        return new Ray(player.Position, player.Forward);
    }
}