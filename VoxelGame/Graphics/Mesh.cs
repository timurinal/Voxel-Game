using System.Collections.Concurrent;
using OpenTK.Mathematics;
using VoxelGame;
using VoxelGame.Maths;
using VoxelGame.Threading;
using Vector2 = VoxelGame.Maths.Vector2;
using Vector3 = VoxelGame.Maths.Vector3;
using Vector4 = OpenTK.Mathematics.Vector4;

namespace VoxelGame.Graphics;

public sealed class Mesh : IRenderable
{
    public Transform Transform;
    public Material Material;

    public Mesh(Material material)
    {
        Transform = new();
        Material = material;
        
        // Generate the vertex array from now as it doesn't change
        // only the data associated to it changes
        _vao = GL.GenVertexArray();
    }

    public Vector3[] Vertices
    {
        get => _vertices;
        set
        {
            _vertices = value;
            _isUpdated = false;
        }
    }
    
    public Vector3[] Normals
    {
        get => _normals;
        set
        {
            _normals = value;
            _isUpdated = false;
        }
    }
    
    public Vector3[] Tangents
    {
        get => _tangents;
        set
        {
            _tangents = value;
            _isUpdated = false;
        }
    }

    public Colour[] Colours
    {
        get => _colours;
        set
        {
            _colours = value;
            _isUpdated = false;
        }
    }
    
    public Vector2[] Uvs
    {
        get => _uvs;
        set
        {
            _uvs = value;
            _isUpdated = false;
        }
    }
    
    public int[] Triangles
    {
        get => _triangles;
        set
        {
            _triangles = value;
            _isUpdated = false;
        }
    }
    
    private Vector3[] _vertices;
    private Vector3[] _normals;
    private Vector3[] _tangents;
    private Colour[] _colours;
    private Vector2[] _uvs;
    private int[] _triangles;

    private int _vertexCount, _triangleCount;

    private int _vao, _vbo, _ebo;
    
    private bool _isUpdated = false;

    private void ConstructMesh()
    {
        _isUpdated = true;
        
        // Bind the vertex array
        GL.BindVertexArray(_vao);

        const int stride = 15; // each vertex has 3 position floats, 3 normal floats, 3 tangent floats, 4 colour floats, 2 uv floats
        float[] data = new float[_vertices.Length * stride];

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
                data[i * stride + 3] = 0;
                data[i * stride + 4] = 0;
                data[i * stride + 5] = 1;
            }
            
            if (_tangents != null && _tangents.Length == _vertices.Length)
            {
                data[i * stride + 6] = _tangents[i].X;
                data[i * stride + 7] = _tangents[i].Y;
                data[i * stride + 8] = _tangents[i].Z;
            }
            else
            {
                data[i * stride + 6] = 1;
                data[i * stride + 7] = 0;
                data[i * stride + 8] = 0;
            }
            
            if (_colours != null && _colours.Length == _vertices.Length)
            {
                data[i * stride + 9] = _colours[i].R;
                data[i * stride + 10] = _colours[i].G;
                data[i * stride + 11] = _colours[i].B;
                data[i * stride + 12] = _colours[i].A;
            }
            else
            {
                data[i * stride + 9] = 0;
                data[i * stride + 10] = 1;
                data[i * stride + 11] = 0;
                data[i * stride + 12] = 1f;
            }

            if (_uvs != null && _uvs.Length == _vertices.Length)
            {
                data[i * stride + 13] = _uvs[i].X;
                data[i * stride + 14] = _uvs[i].Y;
            }
            else
            {
                data[i * stride + 13] = 0;
                data[i * stride + 14] = 0;
            }
        }
        
        // Generate, bind, and set the data of the vertex buffer
        _vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float), data, BufferUsageHint.DynamicDraw);
        
        // Setup vertex attributes
        
        // Get the locations of the attributes
        // so they can appear in any order in the shader file and it will still work
        int posLoc = Material.Shader.GetAttribLocation("vPosition");
        int norLoc = Material.Shader.GetAttribLocation("vNormal");
        int tanLoc = Material.Shader.GetAttribLocation("vTangent");
        int colLoc = Material.Shader.GetAttribLocation("vColour");
        int uvLoc = Material.Shader.GetAttribLocation("vUv");
        GL.VertexAttribPointer(posLoc, 3, VertexAttribPointerType.Float, false, stride * sizeof(float), 0 * sizeof(float));
        GL.VertexAttribPointer(norLoc, 3, VertexAttribPointerType.Float, false, stride * sizeof(float), 3 * sizeof(float));
        GL.VertexAttribPointer(tanLoc, 3, VertexAttribPointerType.Float, false, stride * sizeof(float), 6 * sizeof(float));
        GL.VertexAttribPointer(colLoc, 4, VertexAttribPointerType.Float, false, stride * sizeof(float), 9 * sizeof(float));
        GL.VertexAttribPointer(uvLoc , 2, VertexAttribPointerType.Float, false, stride * sizeof(float), 13 * sizeof(float));
        GL.EnableVertexAttribArray(posLoc);
        GL.EnableVertexAttribArray(norLoc);
        GL.EnableVertexAttribArray(tanLoc);
        GL.EnableVertexAttribArray(colLoc);
        GL.EnableVertexAttribArray(uvLoc);
        
        // Generate, bind, and set the data of the element buffer
        _ebo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _triangles.Length * sizeof(int), _triangles, BufferUsageHint.DynamicDraw);
        
        // Unbind the vertex array to prevent accidental modifications
        GL.BindVertexArray(0);

        _vertexCount = _vertices.Length;
        _triangleCount = _triangles.Length;
    }

    public void Render(Camera camera)
    {
        Render(camera.ProjectionMatrix, camera.ViewMatrix);
    }

    public void Render(Matrix4 m_projview)
    {
        if (!_isUpdated)
            ConstructMesh();
        
        // Set shader uniforms here
        Material.Shader.Use();
        Material.Shader.SetMatrix("m_pv", ref m_projview, autoUse: false);
        Matrix4 m_model = Transform.GenerateModelMatrix();
        Material.Shader.SetMatrix("m_model", ref m_model, autoUse: false);
        
        // Bind and render the vertex array
        GL.BindVertexArray(_vao);
        GL.DrawElements(PrimitiveType.Triangles, _triangleCount, DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0);
        
        Engine.VertexCount += _vertexCount;
        Engine.TriangleCount += _triangleCount / 3;
    }
    
    public void Render(Matrix4 m_proj, Matrix4 m_view)
    {
        if (!_isUpdated)
            ConstructMesh();
        
        // Set shader uniforms here
        Material.Shader.Use();
        Material.Shader.SetMatrix("m_proj", ref m_proj, autoUse: false);
        Material.Shader.SetMatrix("m_view", ref m_view, autoUse: false);
        Matrix4 m_model = Transform.GenerateModelMatrix();
        Material.Shader.SetMatrix("m_model", ref m_model, autoUse: false);
        
        // Bind and render the vertex array
        GL.BindVertexArray(_vao);
        GL.DrawElements(PrimitiveType.Triangles, _triangleCount, DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0);
        
        Engine.VertexCount += _vertexCount;
        Engine.TriangleCount += _triangleCount / 3;
    }
    
    public void RecalculateNormals()
    {
        // Initialize normals and tangents
        _normals = new Vector3[_vertices.Length];
        _tangents = new Vector3[_vertices.Length];

        // Step through each triangle and calculate face normals
        for (int i = 0; i < _triangles.Length; i += 3)
        {
            int index0 = _triangles[i];
            int index1 = _triangles[i + 1];
            int index2 = _triangles[i + 2];

            Vector3 vertex0 = _vertices[index0];
            Vector3 vertex1 = _vertices[index1];
            Vector3 vertex2 = _vertices[index2];

            Vector2 uv0 = _uvs[index0];
            Vector2 uv1 = _uvs[index1];
            Vector2 uv2 = _uvs[index2];

            Vector3 side1 = vertex1 - vertex0;
            Vector3 side2 = vertex2 - vertex0;

            Vector2 deltaUV1 = uv1 - uv0;
            Vector2 deltaUV2 = uv2 - uv0;

            float f = 1.0f / (deltaUV1.X * deltaUV2.Y - deltaUV2.X * deltaUV1.Y);

            Vector3 faceTangent = new Vector3
            {
                X = f * (deltaUV2.Y * side1.X - deltaUV1.Y * side2.X),
                Y = f * (deltaUV2.Y * side1.Y - deltaUV1.Y * side2.Y),
                Z = f * (deltaUV2.Y * side1.Z - deltaUV1.Y * side2.Z),
            };

            Vector3 faceNormal = Vector3.Cross(side1, side2).Normalized;

            // Add the tangent and normal to each vertex's tangent and normal 
            _normals[index0] += faceNormal;
            _normals[index1] += faceNormal;
            _normals[index2] += faceNormal;

            _tangents[index0] += faceTangent;
            _tangents[index1] += faceTangent;
            _tangents[index2] += faceTangent;
        }

        // Normalize all vertex normals and tangents
        for (int i = 0; i < _normals.Length; i++)
        {
            _normals[i] = -_normals[i].Normalized;
            _tangents[i] = _tangents[i].Normalized;
        }
    }

    internal void SortTriangles(Vector3 cameraPos)
    {
        SortedList<(float distance, int index), Triangle> sortedTriangles = new(new DescComparer<(float, int)>());
        ConcurrentArray<((float distance, int index), Triangle tri)> arr = new(_triangles.Length / 3);

        Matrix4 m_model = Transform.GenerateModelMatrix();
        
        Parallel.For(0, _triangles.Length / 3, i =>
        {
            int triIndex = i * 3;
            
            // Extract actual vertex indices from the _triangles array
            int index0 = _triangles[triIndex];
            int index1 = _triangles[triIndex + 1];
            int index2 = _triangles[triIndex + 2];

            Triangle triangle = new Triangle(index0, index1, index2);

            var points = triangle.GetPoints(_vertices);
            Vector4 p1 = new Vector4(points.p1, 1.0f) * m_model;
            Vector4 p2 = new Vector4(points.p2, 1.0f) * m_model;
            Vector4 p3 = new Vector4(points.p3, 1.0f) * m_model;
            
            Vector3 centerPoint = (p1.Xyz + p2.Xyz + p3.Xyz) / 3f;

            float sqrDst = Vector3.SqrDistance(centerPoint, cameraPos);

            arr[i] = ((sqrDst, triIndex / 3), triangle);
            // nonSortedTriangles.TryAdd((sqrDst, triIndex / 3), triangle);
        });
        
        foreach (var triangle in arr)
        {
            sortedTriangles.Add(triangle.Item1, triangle.tri);
        }

        int idx = 0;
        foreach (var triangle in sortedTriangles.Values)
        {
            _triangles[idx] = triangle.a;
            _triangles[idx + 1] = triangle.b;
            _triangles[idx + 2] = triangle.c;
            idx += 3;
        }

        _isUpdated = false; // Reset the update flag to force reconstruction
    }

    struct Triangle
    {
        public int a, b, c;

        public Triangle(int a, int b, int c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }
        
        public (Vector3 p1, Vector3 p2, Vector3 p3) GetPoints(Vector3[] vertices) => (vertices[a], vertices[b], vertices[c]);
    }
}