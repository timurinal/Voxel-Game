Voxel-Game
---

A voxel game (like Minecraft) made in C# using OpenTK. Shaders are written in GLSL<br>
Currently very much a WIP

Features
---

- Infinite procedural terrain in each direction
- Custom-made lighting engine with realtime shadows for directional lights
- Fast, multithreaded chunk builder

How to use
---

**IT IS STRONGLY RECOMMENDED THAT YOU HAVE THE LATEST VERSION OF YOUR GPU DRIVERS BEFORE CONTINUING**

**Will likely only work on Windows, but feel free to try on Unix-like**

### Compile source
- Currently the only way of running the game is by compiling the project yourself
- Start by ensuring you have .NET 8 installed. You can get it from the [official .NET site](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- Clone the repository using Git with the following command or by downloading the zip file by going to Code > Download ZIP
  ```bash
  git clone https://github.com/timurinal/Voxel-Game.git
  ```
- Open the .sln file in an IDE of your choice or run the following command under Voxel-Game/VoxelGame
  ```
  dotnet run
  ```
