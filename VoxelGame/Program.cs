using OpenTK.Windowing.Desktop;

namespace VoxelGame;

class Program
{
    static void Main(string[] args)
    {
        GameWindowSettings gws = new()
        {
            UpdateFrequency = 240
        };

        NativeWindowSettings nws = new()
        {
            ClientSize = new(1280, 720),
            Title = "Voxel Game"
        };

        using var engine = new Engine(gws, nws);
        engine.Run();
    }
}