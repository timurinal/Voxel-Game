namespace VoxelGame.Rendering;

public struct Shader
{
    private int _id;

    public static Shader Load(string vertexShaderPath, string fragmentShaderPath)
    {
        int id = GL.CreateProgram();
        
        SubShader vertexShader = SubShader.Load(vertexShaderPath, ShaderType.VertexShader);
        SubShader fragmentShader = SubShader.Load(fragmentShaderPath, ShaderType.FragmentShader);
        
        GL.AttachShader(id, vertexShader._id);
        GL.AttachShader(id, fragmentShader._id);
        
        GL.LinkProgram(id);
        
        GL.DetachShader(id, vertexShader._id);
        GL.DetachShader(id, fragmentShader._id);
        
        GL.DeleteShader(vertexShader._id);
        GL.DeleteShader(fragmentShader._id);

        string log = GL.GetProgramInfoLog(id);
        if (!string.IsNullOrEmpty(log))
            throw new ShaderErrorException(log);

        return new Shader { _id = id };
    }

    internal void Use()
    {
        GL.UseProgram(_id);
    }

    public int GetUniformLocation(string uniformName) => GL.GetUniformLocation(_id, uniformName);
    
    private struct SubShader
    {
        internal int _id;

        public static SubShader Load(string path, ShaderType type)
        {
            int id = GL.CreateShader(type);

            if (!File.Exists(path))
                throw new FileNotFoundException(path);
            
            string source = File.ReadAllText(path);
            
            GL.ShaderSource(id, source);
            GL.CompileShader(id);

            string log = GL.GetShaderInfoLog(id);
            if (!string.IsNullOrEmpty(log))
                throw new ShaderErrorException(path, log);

            return new SubShader { _id = id };
        }
    }
    
    private class ShaderErrorException : Exception
    {
        public ShaderErrorException(string error) : base($"Error when compiling shader program: {error}")
        {
        }
        public ShaderErrorException(string path, string error) : base($"Error when compiling shader at path {path}: {error}")
        {
        }
    }
}