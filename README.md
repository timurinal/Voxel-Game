Voxel-Game
---

A voxel game (like Minecraft) made in C# using OpenTK. Shaders are written in GLSL

Features
---

- Infinite procedural terrain in each direction
- Custom-made lighting engine with realtime shadows for directional lights, and a point light system with specular highlights

How to use
---

**IT IS STRONGLY RECOMMENDED THAT YOU HAVE THE LATEST VERSION OF YOUR GPU DRIVERS BEFORE CONTINUING**

### Ready-to-play release
First, start by ensuring you have .NET 8 installed by downloading it from the [official .NET site](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)<br>
Then, download one of the releases in Github and run the executable.<br>
There is a shortcut in the root of the folder that runs the correct executable. Binaries are found in the /bin/ folder as well as runtime assets being located in /bin/Assets

### Source code
Start by cloning the repository.<br>
The project was made in JetBrains Rider and the project-specific IDE files are included so formatting may differ between IDEs<br>
Any required packages should automatically be installed by your IDE's package manager.
<br>
**If you want to contribute, please ensure all code follows the [naming guidelines](https://github.com/timurinal/Voxel-Game/blob/main/VoxelGame/README.md)**

Planned for the future
---

Progress is tracked on a [Trello board](https://trello.com/b/lt8gN72f)<br>
- Physics engine that allows for player gravity
- Debug information on screen
- UI system
- Non blocking chunk generation
- Lighting improvements
- Point and spot light shadows
- Shadow cascading
- Biomes
- Chunk saving and loading
