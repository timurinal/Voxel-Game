using System.Reflection;

namespace VoxelGame;

public static class Environment
{
    public static Assembly Current { get; private set; }

    static Environment()
    {
        Current = Assembly.GetExecutingAssembly();
    }

    /// <summary>
    /// Loads a manifest resource from the given location.
    /// </summary>
    /// <param name="location">The location of the assembly.</param>
    /// <param name="stream">The stream that represents the loaded manifest resource.</param>
    /// <returns>True if the manifest resource stream was successfully loaded; otherwise, false.</returns>
    public static bool LoadAssemblyStream(string location, out Stream stream)
    {
        var str = Current.GetManifestResourceStream(location);
        
        if (str == null)
        {
            stream = null;
            return false;
        }
        else
        {
            stream = str;
            return true;
        }
    }
}