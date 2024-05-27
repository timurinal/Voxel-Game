namespace VoxelGame.Rendering;

public class GLException : Exception
{
    public GLException(string message) : base(message)
    {
    }

    public GLException(ErrorCode code, string message) : base($"GL Error Code: {code}. {message}")
    {
    }
    
    public GLException(ErrorCode code) : base($"GL Error: {code}")
    {
    }
}