using OpenTK.Mathematics;
using Vector2 = VoxelGame.Maths.Vector2;
using Vector3 = VoxelGame.Maths.Vector3;

namespace VoxelGame.Rendering;

public static class UIRenderer
{
    private static List<UIElement> _uiElements = new();

    public static void CreateQuad(Vector2 position, Vector3 size)
    {
        var element = new UIElement(Shader.StandardUIShader)
        {
            Vertices =
            [
                new(-0.5f, -0.5f, 0.0f),
                new(-0.5f, 0.5f, 0.0f),
                new(0.5f, -0.5f, 0.0f),
                new(0.5f, 0.5f, 0.0f),
            ],
            Uvs = 
            [
                new(0, 0),
                new(0, 1),
                new(1, 0),
                new(1, 1),
            ],
            Triangles =
            [
                0, 2, 1,
                2, 3, 1,
            ]
        };
        
        _uiElements.Add(element);
    }

    internal static void Render(Player player)
    {
        foreach (var uiElement in _uiElements)
        {
            uiElement.Render(player);
        }
    }

    struct UIElement : IRenderable
    {
        public RectTransform Transform;
        
        public Vector3[] Vertices
        {
            get => _vertices;
            set
            {
                _vertices = value;
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

        private bool _isUpdated;
        
        private Vector3[] _vertices;
        private Vector2[] _uvs;
        private int[] _triangles;

        private Shader _shader;

        private float[] data;

        private int _vao, _vbo, _ebo;

        private Matrix4 m_model;

        public UIElement(Shader shader)
        {
            _shader = shader;
            _vao = GL.GenVertexArray();
            Transform = new();
        }

        private void Setup()
        {
            const int stride = 5; // each vertex has 3 position floats, and 2 uv floats

            data = new float[_vertices.Length * stride];
            for (int i = 0; i < _vertices.Length; i++)
            {
                data[i * stride + 0] = _vertices[i].X;
                data[i * stride + 1] = _vertices[i].Y;
                data[i * stride + 2] = _vertices[i].Z;

                if (_uvs != null && _uvs.Length == _vertices.Length)
                {
                    data[i * stride + 3] = _uvs[i].X;
                    data[i * stride + 4] = _uvs[i].Y;
                }
                else
                {
                    data[i * stride + 3] = 0;
                    data[i * stride + 4] = 0;
                }
            }
            
            GL.BindVertexArray(_vao);

            // setup vbo
            _vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float), data, BufferUsageHint.StaticDraw);
            
            // setup vertex attributes
            int vLoc = _shader.GetAttribLocation("vPosition");
            int uLoc = _shader.GetAttribLocation("vUv");
            GL.VertexAttribPointer(vLoc, 3, VertexAttribPointerType.Float, false, stride * sizeof(float), 0 * sizeof(float));
            GL.VertexAttribPointer(uLoc, 2, VertexAttribPointerType.Float, false, stride * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(vLoc);
            GL.EnableVertexAttribArray(uLoc);
            
            // setup ebo
            _ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, _triangles.Length * sizeof(int), _triangles, BufferUsageHint.StaticDraw);

            _isUpdated = true;
        }

        public (int vertexCount, int triangleCount) Render(Player player)
        {
            if (!_isUpdated)
                Setup();

            m_model = Transform.GetModelMatrix();
            
            GL.BindVertexArray(_vao);
            _shader.SetUniform("m_proj", ref player.ProjectionMatrix);
            _shader.SetUniform("m_view", ref player.ViewMatrix);
            _shader.SetUniform("m_model", ref m_model);
            _shader.Use();
            GL.DrawElements(PrimitiveType.Triangles, _triangles.Length, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);

            return (0, 0);
        }
    }
}