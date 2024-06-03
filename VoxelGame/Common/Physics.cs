using VoxelGame.Maths;
using VoxelGame.Rendering;

namespace VoxelGame;

public static class Physics
{
    public static Vector3 Gravity { get; set; } = new(0, -9.81f, 0);

    public static void ResolveCollisions(Player player, AABB[] staticObjects)
    {
        foreach (var staticObject in staticObjects)
        {
            if (IsColliding(player.Collider, staticObject))
            {
                ResolveCollision(player, staticObject);
            }
        }
    }

    private static void ResolveCollision(Player player, AABB staticObject)
    {
        Vector3 moveDir = Vector3.Zero;

        // Calculate overlap on each axis
        float overlapX = (player.Collider.Max.X - staticObject.Min.X < staticObject.Max.X - player.Collider.Min.X)
            ? player.Collider.Max.X - staticObject.Min.X
            : staticObject.Max.X - player.Collider.Min.X;

        float overlapY = (player.Collider.Max.Y - staticObject.Min.Y < staticObject.Max.Y - player.Collider.Min.Y)
            ? player.Collider.Max.Y - staticObject.Min.Y
            : staticObject.Max.Y - player.Collider.Min.Y;

        float overlapZ = (player.Collider.Max.Z - staticObject.Min.Z < staticObject.Max.Z - player.Collider.Min.Z)
            ? player.Collider.Max.Z - staticObject.Min.Z
            : staticObject.Max.Z - player.Collider.Min.Z;

        // Find the axis with the smallest penetration
        if (overlapX < overlapY && overlapX < overlapZ)
        {
            // Move the player in X direction
            if (player.Collider.Center.X < staticObject.Center.X)
                moveDir = new Vector3(-overlapX, 0, 0);
            else
                moveDir = new Vector3(overlapX, 0, 0);
        }
        else if (overlapY < overlapX && overlapY < overlapZ)
        {
            // Move the player in Y direction
            if (player.Collider.Center.Y < staticObject.Center.Y)
                moveDir = new Vector3(0, -overlapY, 0);
            else
                moveDir = new Vector3(0, overlapY, 0);
        }
        else
        {
            // Move the player in Z direction
            if (player.Collider.Center.Z < staticObject.Center.Z)
                moveDir = new Vector3(0, 0, -overlapZ);
            else
                moveDir = new Vector3(0, 0, overlapZ);
        }

        // Move the player
        player.Move(moveDir);
    }
    
    private static bool IsColliding(AABB player, AABB staticObject)
    {
        return (player.Min.X <= staticObject.Max.X && player.Max.X >= staticObject.Min.X) &&
               (player.Min.Y <= staticObject.Max.Y && player.Max.Y >= staticObject.Min.Y) &&
               (player.Min.Z <= staticObject.Max.Z && player.Max.Z >= staticObject.Min.Z);
    }
}