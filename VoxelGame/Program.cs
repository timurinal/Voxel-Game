using OpenTK.Windowing.Desktop;

namespace VoxelGame;

class Program
{
    static void Main(string[] args)
    {
        GameWindowSettings gws = new()
        {
            UpdateFrequency = -1
        };

        NativeWindowSettings nws = new()
        {
            ClientSize = new(1280, 720), 
            MinimumClientSize = new(640, 360),
            Title = "Voxel Game",
            NumberOfSamples = 0
        };

        using var engine = new Engine(gws, nws);
        engine.Run();
    }
}