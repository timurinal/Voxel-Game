using OpenTK.Windowing.Desktop;

namespace VoxelGame;

class Program
{
    static void Main(string[] args)
    {
        // int[] arr = 
        // {
        //     0, 1, 2
        // };
        //
        // foreach (var voxel in VoxelData.Voxels)
        // {
        //     Console.WriteLine(voxel.ToString());
        // }
        // return;
        
        // FontReader.ParseFont("Assets/Fonts/JetBrainsMono-bold.ttf");
        // return;
        
        GameWindowSettings gws = new()
        {
            UpdateFrequency = -1
        };

        NativeWindowSettings nws = new()
        {
            ClientSize = new(1280, 720), 
            MinimumClientSize = new(640, 360),
            Title = "Voxel Game",
            NumberOfSamples = 16
        };

        using var engine = new Engine(gws, nws);
        engine.Run();
    }
}