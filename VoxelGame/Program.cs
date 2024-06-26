﻿using OpenTK.Windowing.Desktop;

namespace VoxelGame;

class Program
{
    static void Main(string[] args)
    {
        // foreach (var voxel in VoxelData.Voxels)
        // {
        //     Console.WriteLine(voxel.ToString());
        // }
        // return;
        
        GameWindowSettings gws = new()
        {
            UpdateFrequency = 240
        };

        NativeWindowSettings nws = new()
        {
            ClientSize = new(1280, 720), 
            MinimumClientSize = new(640, 360),
            Title = "Voxel Game"
        };

        using var engine = new Engine(gws, nws);
        engine.Run();
    }
}