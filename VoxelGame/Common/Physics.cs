using VoxelGame.Maths;

namespace VoxelGame;

public static class Physics
{
    public static Vector3 Gravity { get; set; } = new(0, -9.81f, 0);
    
    public static void ResolveCollision(AABB moving, AABB stationary, ref Vector3 velocity)
    {
        if (moving.Intersects(stationary))
        {
            Vector3 penetrationDepth = moving.GetPenetrationDepth(stationary);

            if (penetrationDepth.X < penetrationDepth.Y && penetrationDepth.X < penetrationDepth.Z)
            {
                // Resolve collision in X axis
                if (moving.Min.X < stationary.Min.X)
                {
                    moving.Min.X -= penetrationDepth.X;
                    moving.Max.X -= penetrationDepth.X;
                }
                else
                {
                    moving.Min.X += penetrationDepth.X;
                    moving.Max.X += penetrationDepth.X;
                }

                velocity.X = 0; // Stop horizontal movement
            }
            else if (penetrationDepth.Y < penetrationDepth.X && penetrationDepth.Y < penetrationDepth.Z)
            {
                // Resolve collision in Y axis
                if (moving.Min.Y < stationary.Min.Y)
                {
                    moving.Min.Y -= penetrationDepth.Y;
                    moving.Max.Y -= penetrationDepth.Y;
                }
                else
                {
                    moving.Min.Y += penetrationDepth.Y;
                    moving.Max.Y += penetrationDepth.Y;
                }

                velocity.Y = 0; // Stop vertical movement
            }
            else
            {
                // Resolve collision in Z axis
                if (moving.Min.Z < stationary.Min.Z)
                {
                    moving.Min.Z -= penetrationDepth.Z;
                    moving.Max.Z -= penetrationDepth.Z;
                }
                else
                {
                    moving.Min.Z += penetrationDepth.Z;
                    moving.Max.Z += penetrationDepth.Z;
                }

                velocity.Z = 0; // Stop depth movement
            }
        }
    }
}