using OpenTK.Windowing.GraphicsLibraryFramework;

namespace VoxelGame;

public static class Input
{
    internal static KeyboardState _keyboardState;

    public static bool GetKey(Keys key) => _keyboardState.IsKeyDown(key);
    public static bool GetKeyDown(Keys key) => _keyboardState.IsKeyPressed(key);
    public static bool GetKeyUp(Keys key) => _keyboardState.IsKeyReleased(key);
}