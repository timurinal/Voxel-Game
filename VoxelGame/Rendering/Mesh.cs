using OpenTK.Mathematics;
using Vector3 = VoxelGame.Maths.Vector3;

namespace VoxelGame.Rendering;

public class Mesh : IRenderable
{
    public string Name;

    public Transform Transform;

    public Vector3[] Vertices
    {
        get => _vertices;
        set
        {
            _vertices = value;
            isMeshUpdated = false;
        } 
    }
    public Vector3[] Normals
    {
        get => _normals;
        set
        {
            _normals = value;
            isMeshUpdated = false;
        } 
    }
    
    public Vector2[] Uvs
    {
        get => _uvs;
        set
        {
            _uvs = value;
            isMeshUpdated = false;
        } 
    }

    public Colour[] Colours
    {
        get => _colours;
        set
        {
            _colours = value;
            isMeshUpdated = false;
        }
    }
    public int[] Triangles
    {
        get => _triangles;
        set
        {
            _triangles = value;
            isMeshUpdated = false;
        } 
    }

    private Vector3[] _vertices;
    private Vector3[] _normals;
    private Vector2[] _uvs;
    private Colour[] _colours;
    private int[] _triangles;

    private int _triangleCount = 0;
    
    private int _vao, _vbo, _ebo;

    private Shader _shader;

    private bool isMeshUpdated = false;
    
    private Matrix4 m_model;
    
    public Mesh(string name, Shader shader)
    {
        this.Name = name;
        _shader = shader;
        Transform = new(Vector3.Zero, Vector3.Zero, Vector3.One);
        
        InitMesh();
    }
    
    public Mesh(Shader shader)
    {
        Name = "Mesh";
        _shader = shader;
        Transform = new(Vector3.Zero, Vector3.Zero, Vector3.One);
        
        InitMesh();
    }
    
    public Mesh(string name, Shader shader, Vector3 postion, Vector3 rotation, Vector3 scale)
    {
        this.Name = name;
        _shader = shader;
        Transform = new(postion, rotation, scale);
        
        InitMesh();
    }
    
    public Mesh(Shader shader, Vector3 postion, Vector3 rotation, Vector3 scale)
    {
        Name = "Mesh";
        _shader = shader;
        Transform = new(postion, rotation, scale);
        
        InitMesh();
    }

    private void InitMesh()
    {
        // all that needs to be initialised is the VAO
        _vao = GL.GenVertexArray();
    }

    private void SetupMesh()
    {
        GL.BindVertexArray(_vao);

        int stride = 12; // each vertex has 3 position floats, 3 normal floats, 2 uv floats, and 4 colour floats
        float[] data = new float[stride * _vertices.Length];

        for (int i = 0; i < _vertices.Length; i++)
        {
            data[i * stride + 0] = _vertices[i].X;
            data[i * stride + 1] = _vertices[i].Y;
            data[i * stride + 2] = _vertices[i].Z;
            
            if (_normals != null && _normals.Length == _vertices.Length)
            {
                data[i * stride + 3] = _normals[i].X;
                data[i * stride + 4] = _normals[i].Y;
                data[i * stride + 5] = _normals[i].Z;
            }
            else
            {
                data[i * stride + 3] = 1;
                data[i * stride + 4] = 0;
                data[i * stride + 5] = 1;
            }
            
            if (_uvs != null && _uvs.Length == _vertices.Length)
            {
                data[i * stride + 6] = _uvs[i].X;
                data[i * stride + 7] = _uvs[i].Y;
            }
            else
            {
                data[i * stride + 6] = 1;
                data[i * stride + 7] = 0;
            }

            if (_colours != null && _colours.Length == _vertices.Length)
            {
                data[i * stride + 8]  = _colours[i].R;
                data[i * stride + 9]  = _colours[i].G;
                data[i * stride + 10] = _colours[i].B;
                data[i * stride + 11] = _colours[i].A;
            }
            else
            {
                data[i * stride + 8]  = 1;
                data[i * stride + 9]  = 0;
                data[i * stride + 10] = 1;
                data[i * stride + 11] = 1;
            }
        }
        
        // generate and bind the vbo
        _vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float), data, BufferUsageHint.StaticDraw);
        
        // setup vertex attributes
        int vLoc = _shader.GetAttribLocation("vPosition");
        int nLoc = _shader.GetAttribLocation("vNormal");
        int tLoc = _shader.GetAttribLocation("vTexcoord");
        int cLoc = _shader.GetAttribLocation("vColour");
        GL.VertexAttribPointer(vLoc, 3, VertexAttribPointerType.Float, false, stride * sizeof(float), 0 * sizeof(float));
        GL.VertexAttribPointer(nLoc, 3, VertexAttribPointerType.Float, false, stride * sizeof(float), 3 * sizeof(float));
        GL.VertexAttribPointer(tLoc, 2, VertexAttribPointerType.Float, false, stride * sizeof(float), 6 * sizeof(float));
        GL.VertexAttribPointer(cLoc, 4, VertexAttribPointerType.Float, false, stride * sizeof(float), 8 * sizeof(float));
        GL.EnableVertexAttribArray(vLoc);
        GL.EnableVertexAttribArray(nLoc);
        GL.EnableVertexAttribArray(tLoc);
        GL.EnableVertexAttribArray(cLoc);
        
        // generate and bind the ebo
        _ebo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _triangles.Length * sizeof(int), _triangles,
            BufferUsageHint.StaticDraw);

        _triangleCount = _triangles.Length;
        
        // unbind the vertex array
        GL.BindVertexArray(0);

        isMeshUpdated = true;
    }

    public (int vertexCount, int triangleCount) Render(Player player)
    {
        if (!isMeshUpdated)
            SetupMesh();

        m_model = Transform.GetModelMatrix();
        
        _shader.Use();
        _shader.SetUniform("m_proj" , ref player.ProjectionMatrix);
        _shader.SetUniform("m_view" , ref player.ViewMatrix);
        _shader.SetUniform("m_model", ref m_model);
        
        GL.BindVertexArray(_vao);
        GL.DrawElements(PrimitiveType.Triangles, _triangleCount, DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0);

        return (Vertices.Length, _triangleCount / 3);
    }
}