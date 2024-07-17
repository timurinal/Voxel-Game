using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Vector2 = VoxelGame.Maths.Vector2;
using Vector3 = VoxelGame.Maths.Vector3;

namespace VoxelGame.Rendering;

public static class UIRenderer
{
    private static List<UIElement> _uiElements = new();
    
    public static void CreateQuad(Vector3 position, Vector3 size, bool useProjection = true, int i = 0)
    {
        Vector2 uv00 = FontAtlas.GetUVForFont(i, 0, 1);
        Vector2 uv01 = FontAtlas.GetUVForFont(i, 0, 0);
        Vector2 uv10 = FontAtlas.GetUVForFont(i, 1, 1);
        Vector2 uv11 = FontAtlas.GetUVForFont(i, 1, 0);
        
        var element = new UIElement(Shader.StandardUIShader, useProjection)
        {
            Vertices =
            [
                new Vector3(-0.5f, -0.5f, -25.0f) + position,
                new Vector3(-0.5f, 0.5f, -25.0f) + position,
                new Vector3(0.5f, -0.5f, -25.0f) + position,
                new Vector3(0.5f, 0.5f, -25.0f) + position,
            ],
            Uvs = 
            [
                uv00, uv01, uv10, uv11
            ],
            Triangles =
            [
                0, 1, 2,
                2, 1, 3,
            ]
        };
        
        _uiElements.Add(element);
    }

    public static void Update()
    {
        // if (Input.GetKeyDown(Keys.Right))
        // {
        //     I++;
        //     
        //     _uiElements.Clear();
        //     
        //     CreateQuad(Vector2.Zero, Vector3.Zero);
        // }
        //
        // if (Input.GetKeyDown(Keys.Left))
        // {
        //     I--;
        //     
        //     _uiElements.Clear();
        //     
        //     CreateQuad(Vector2.Zero, Vector3.Zero);
        // }
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
        public bool useProjection;
        
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

        public UIElement(Shader shader, bool useProjection)
        {
            _shader = shader;
            _vao = GL.GenVertexArray();
            Transform = new();

            this.useProjection = useProjection;
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
            _shader.Use();
            
            _shader.SetUniform("m_proj", ref player.ProjectionMatrix, autoUse: false);
            _shader.SetUniform("m_view", ref player.ViewMatrix, autoUse: false);
            _shader.SetUniform("m_model", ref m_model, autoUse: false);
            
            _shader.SetUniform("useProjection", useProjection ? 1 : 0, autoUse: false);
            
            FontAtlas.FontTexture.Use(TextureUnit.Texture7);
            _shader.SetUniform("Texture", 7);
            
            GL.DrawElements(PrimitiveType.Triangles, _triangles.Length, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);

            return (0, 0);
        }
    }
}