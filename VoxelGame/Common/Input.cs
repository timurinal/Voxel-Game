using OpenTK.Windowing.GraphicsLibraryFramework;

namespace VoxelGame;

// Utility class for easily getting keyboard input
public static class Input
{
    internal static KeyboardState _kbState { get; set; }

    public static bool GetKey(Keys key) => _kbState.IsKeyDown(key);
    public static bool GetKeyDown(Keys key) => _kbState.IsKeyPressed(key);
    public static bool GetKeyUp(Keys key) => _kbState.IsKeyReleased(key);
}