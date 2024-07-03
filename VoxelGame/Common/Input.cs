using OpenTK.Windowing.GraphicsLibraryFramework;

namespace VoxelGame;

public static class Input
{
    internal static KeyboardState _keyboardState;

    public static bool GetKey(Keys key) => _keyboardState.IsKeyDown(key);
    public static bool GetKeyDown(Keys key) => _keyboardState.IsKeyPressed(key);
    public static bool GetKeyUp(Keys key) => _keyboardState.IsKeyReleased(key);

    public static float GetAxis(string axisName)
    {
        if (axisName == "horizontal")
        {
            float val = 0;
            if (GetKey(Keys.D)) val = 1;
            if (GetKey(Keys.A)) val = -1;

            return val;
        }
        
        if (axisName == "vertical")
        {
            float val = 0;
            if (GetKey(Keys.W)) val = 1;
            if (GetKey(Keys.S)) val = -1;

            return val;
        }

        return 0;
    }
}