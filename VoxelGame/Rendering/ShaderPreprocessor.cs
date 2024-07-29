using System.Text.RegularExpressions;

namespace VoxelGame.Rendering;

internal static class ShaderPreprocessor
{
    public const int MaxIncludeDepth = 5;

    public static string PreprocessShaderSource(string source)
    {
        return ProcessIncludes(source, 0);
    }

    private static string ProcessIncludes(string source, int depth)
    {
        if (depth >= MaxIncludeDepth)
        {
            return source;
        }

        var regex = new Regex("#include\\s*<(.+?)>");

        return regex.Replace(source, match =>
        {
            var includePath = match.Groups[1].Value;
                 
            // Assuming includePath is a relative path from the directory of the current shader file
            // Adjust according to your needs
            var includeText = File.ReadAllText(includePath);

            // Recursively process the includes in the included file
            includeText = ProcessIncludes(includeText, depth + 1);

            return includeText;
        });
    }
}