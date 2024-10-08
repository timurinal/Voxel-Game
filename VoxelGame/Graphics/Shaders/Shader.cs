using OpenTK.Mathematics;
using VoxelGame.Graphics;
using Vector2 = VoxelGame.Maths.Vector2;
using Vector3 = VoxelGame.Maths.Vector3;

namespace VoxelGame.Graphics.Shaders;

public struct Shader : IEquatable<Shader>
{
    private int _id;

    public static Shader None => new() { _id = 0 };
    
    internal static Shader StandardShader
    {
        get
        {
            int id = GL.CreateProgram();

            const string vertexSource =
                "#version 330 core\n\nlayout (location = 0) in vec3 vPosition;\nlayout (location = 1) in vec3 vNormal;\nlayout (location = 2) in vec4 vColour;\n\nout vec4 fragColour;\n\nuniform mat4 m_proj;\nuniform mat4 m_view;\nuniform mat4 m_model;\n\nvoid main() {\n\tgl_Position = m_proj * m_view * m_model * vec4(vPosition, 1.0);\n\n\tfragColour = vColour;\n}";
            const string fragmentSource =
                "#version 330 core\n\nout vec4 finalColour;\n\nin vec4 fragColour;\n\nvoid main() {\n\tfinalColour = fragColour;\n}";
            
            int vertexId = GL.CreateShader(ShaderType.VertexShader);
            int fragmentId = GL.CreateShader(ShaderType.FragmentShader);
            
            GL.ShaderSource(vertexId, vertexSource);
            GL.CompileShader(vertexId);
            
            {
                string log = GL.GetShaderInfoLog(vertexId);
                if (!string.IsNullOrEmpty(log))
                    throw new ShaderErrorException(log);
            }
            
            GL.ShaderSource(fragmentId, fragmentSource);
            GL.CompileShader(fragmentId);
            
            {
                string log = GL.GetShaderInfoLog(fragmentId);
                if (!string.IsNullOrEmpty(log))
                    throw new ShaderErrorException(log);
            }
        
            GL.AttachShader(id, vertexId);
            GL.AttachShader(id, fragmentId);
        
            GL.LinkProgram(id);
        
            GL.DetachShader(id, vertexId);
            GL.DetachShader(id, fragmentId);
        
            GL.DeleteShader(vertexId);
            GL.DeleteShader(fragmentId);

            {
                string log = GL.GetProgramInfoLog(id);
                if (!string.IsNullOrEmpty(log))
                    throw new ShaderErrorException(log);
            }

            return new Shader { _id = id };
        }
    }
    internal static Shader StandardUIShader
    {
        get
        {
            int id = GL.CreateProgram();

            const string vertexSource =
                "#version 330 core\n\nlayout (location = 0) in vec3 vPosition;\nlayout (location = 1) in vec2 vUv;\n\nout vec4 fragColour;\n\nuniform mat4 m_proj;\nuniform mat4 m_view;\nuniform mat4 m_model;\n\nvoid main() {\n\tgl_Position = m_model * vec4(vPosition, 1.0);\n\n\tfragColour = vec4(vUv, 0.0, 1.0);\n}";
            const string fragmentSource =
                "#version 330 core\n\nout vec4 finalColour;\n\nin vec4 fragColour;\n\nvoid main() {\n\tfinalColour = fragColour;\n}";
            
            int vertexId = GL.CreateShader(ShaderType.VertexShader);
            int fragmentId = GL.CreateShader(ShaderType.FragmentShader);
            
            GL.ShaderSource(vertexId, vertexSource);
            GL.CompileShader(vertexId);
            
            {
                string log = GL.GetShaderInfoLog(vertexId);
                if (!string.IsNullOrEmpty(log))
                    throw new ShaderErrorException(log);
            }
            
            GL.ShaderSource(fragmentId, fragmentSource);
            GL.CompileShader(fragmentId);
            
            {
                string log = GL.GetShaderInfoLog(fragmentId);
                if (!string.IsNullOrEmpty(log))
                    throw new ShaderErrorException(log);
            }
        
            GL.AttachShader(id, vertexId);
            GL.AttachShader(id, fragmentId);
        
            GL.LinkProgram(id);
        
            GL.DetachShader(id, vertexId);
            GL.DetachShader(id, fragmentId);
        
            GL.DeleteShader(vertexId);
            GL.DeleteShader(fragmentId);

            {
                string log = GL.GetProgramInfoLog(id);
                if (!string.IsNullOrEmpty(log))
                    throw new ShaderErrorException(log);
            }

            return new Shader { _id = id };
        }
    }
    internal static Shader GizmoShader
    {
        get
        {
            int id = GL.CreateProgram();

            const string vertexSource =
                "#version 450 core\n\nlayout (location = 0) in vec3 vPosition;\n\nuniform mat4 m_proj;\nuniform mat4 m_view;\nuniform mat4 m_model;\n\nvoid main() {\n\tgl_Position = m_proj * m_view * m_model * vec4(vPosition, 1.0);\n}";
            const string fragmentSource =
                "#version 450 core\n\nout vec4 finalColour;\n\nuniform vec3 Colour;\n\nvoid main() {\n\tfinalColour = vec4(Colour, 1.0);\n}";
            
            int vertexId = GL.CreateShader(ShaderType.VertexShader);
            int fragmentId = GL.CreateShader(ShaderType.FragmentShader);
            
            GL.ShaderSource(vertexId, vertexSource);
            GL.CompileShader(vertexId);
            
            {
                string log = GL.GetShaderInfoLog(vertexId);
                if (!string.IsNullOrEmpty(log))
                    throw new ShaderErrorException(log);
            }
            
            GL.ShaderSource(fragmentId, fragmentSource);
            GL.CompileShader(fragmentId);
            
            {
                string log = GL.GetShaderInfoLog(fragmentId);
                if (!string.IsNullOrEmpty(log))
                    throw new ShaderErrorException(log);
            }
        
            GL.AttachShader(id, vertexId);
            GL.AttachShader(id, fragmentId);
        
            GL.LinkProgram(id);
        
            GL.DetachShader(id, vertexId);
            GL.DetachShader(id, fragmentId);
        
            GL.DeleteShader(vertexId);
            GL.DeleteShader(fragmentId);

            {
                string log = GL.GetProgramInfoLog(id);
                if (!string.IsNullOrEmpty(log))
                    throw new ShaderErrorException(log);
            }

            return new Shader { _id = id };
        }
    }

    public static Shader Load(string vertexShaderPath, string fragmentShaderPath)
    {
        int id = GL.CreateProgram();

        SubShader vertexShader;
        
        vertexShader = SubShader.Load(vertexShaderPath, ShaderType.VertexShader);
        
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

    internal int GetUniformLocation(string uniformName, bool autoUse = true)
    {
        if (autoUse)
            Use();
        return GL.GetUniformLocation(_id, uniformName);
    }

    internal int GetAttribLocation(string attribName, bool autoUse = true)
    {
        if (autoUse)
            Use();
        return GL.GetAttribLocation(_id, attribName);
    }

    public void SetFloat(string uniformName, float v, bool autoUse = true)
    {
        if (autoUse)
            Use();
        GL.Uniform1(GetUniformLocation(uniformName), v);
    }
    public void SetVector2(string uniformName, Vector2 v, bool autoUse = true)
    {
        if (autoUse)
            Use();
        GL.Uniform2(GetUniformLocation(uniformName), v);
    }
    public void SetVector3(string uniformName, Vector3 v, bool autoUse = true)
    {
        if (autoUse)
            Use();
        GL.Uniform3(GetUniformLocation(uniformName), v);
    }
    public void SetVector4(string uniformName, Vector4 v, bool autoUse = true)
    {
        if (autoUse)
            Use();
        GL.Uniform4(GetUniformLocation(uniformName), v);
    }
    public void SetColour(string uniformName, Colour v, bool autoUse = true)
    {
        if (autoUse)
            Use();
        GL.Uniform4(GetUniformLocation(uniformName), v);
    }
    public void SetInt(string uniformName, int v, bool autoUse = true)
    {
        if (autoUse)
            Use();
        GL.Uniform1(GetUniformLocation(uniformName), v);
    }
    public void SetMatrix(string uniformName, ref Matrix4 m, bool transpose = false, bool autoUse = true)
    {
        if (autoUse)
            Use();
        GL.UniformMatrix4(GetUniformLocation(uniformName), transpose, ref m);
    }
    
    private struct SubShader
    {
        internal int _id;

        public static SubShader Load(string path, ShaderType type)
        {
            int id = GL.CreateShader(type);

            if (!File.Exists(path))
                throw new FileNotFoundException(path);
            
            string source = File.ReadAllText(path);
            source = ShaderPreprocessor.PreprocessShaderSource(source);
            
            GL.ShaderSource(id, source);
            GL.CompileShader(id);

            string log = GL.GetShaderInfoLog(id);
            if (!string.IsNullOrEmpty(log))
                throw new ShaderErrorException(path, log);

            return new SubShader { _id = id };
        }
        
        public static SubShader CreateFromSource(string source, ShaderType type)
        {
            int id = GL.CreateShader(type);
            
            GL.ShaderSource(id, source);
            GL.CompileShader(id);

            string log = GL.GetShaderInfoLog(id);
            if (!string.IsNullOrEmpty(log))
                throw new ShaderErrorException(log);

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

    public bool Equals(Shader other)
    {
        return _id == other._id;
    }

    public override bool Equals(object? obj)
    {
        return obj is Shader other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_id);
    }

    public static bool operator ==(Shader a, Shader b)
    {
        return a._id == b._id;
    }
    
    public static bool operator !=(Shader a, Shader b)
    {
        return !(a == b);
    }
}