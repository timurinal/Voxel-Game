using VoxelGame.Maths;
using PhysX;

namespace VoxelGame;

public static class Physics
{
    public static Vector3 Gravity { get; set; } = new(0, -9.81f, 0);

    private static Foundation physxFoundation;
    private static PhysX.Physics physxPhysics;
    private static Scene physxScene;

    internal static void Init()
    {
        physxFoundation = new Foundation();
        physxPhysics = new PhysX.Physics(physxFoundation);
        var sceneDesc = new SceneDesc
        {
            Gravity = Gravity
        };
        physxScene = physxPhysics.CreateScene(sceneDesc);
    }

    private static float accumulatedTime = 0f;

    public static void Update()
    {
        accumulatedTime += Time.DeltaTime;

        while (accumulatedTime >= Time.FixedTimeStep)
        {
            Console.WriteLine("Updated physics.");

            accumulatedTime -= Time.FixedTimeStep;
        }
    }
}