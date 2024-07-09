using OpenTK.Mathematics;
using VoxelGame.Maths;
using Vector3 = VoxelGame.Maths.Vector3;

namespace VoxelGame.Rendering;

public class GizmoBox
{
    private AABB _bounds;

    private Shader _shader;

    private int _vao, _vbo, _ebo;

    public GizmoBox(AABB bounds)
    {
        _vao = GL.GenVertexArray();
        
        _shader = Shader.GizmoShader;
        
        Update(bounds);
    }

    public void Update(AABB bounds)
    {
        _bounds = bounds;

        var vertices = GenerateCubeVertices();
        int[] triangles =
        [
            // Front face
            0, 1, 2,
            0, 2, 3,

            // Back face
            7, 6, 5,
            7, 5, 4,

            // Left face
            4, 1, 0,
            4, 5, 1,

            // Right face
            3, 2, 6,
            3, 6, 7,
        
            // Top face
            1, 6, 2,
            1, 5, 6,
        
            // Bottom face
            4, 3, 7,
            4, 0, 3
        ];

        GL.BindVertexArray(_vao);
        
        _vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * Vector3.Size, vertices, BufferUsageHint.StaticDraw);

        _ebo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, triangles.Length * sizeof(int), triangles, BufferUsageHint.StaticDraw);
        
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        
        GL.BindVertexArray(0);
    }

    public void Render(Player player)
    {
        Render(player, new(0, 0, 1));
    }
    
    public void Render(Player player, Vector3 colour)
    {
        _shader.Use();
        _shader.SetUniform("m_proj", ref player.ProjectionMatrix, autoUse: false);
        _shader.SetUniform("m_view", ref player.ViewMatrix, autoUse: false);
        
        Matrix4 m_model = Matrix4.CreateTranslation(_bounds.Center);
        _shader.SetUniform("m_model", ref m_model, autoUse: false);
        
        _shader.SetUniform("Colour", colour, autoUse: false);
        
        GL.BindVertexArray(_vao);
        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
        GL.Disable(EnableCap.CullFace);
        GL.DrawElements(PrimitiveType.Triangles, 36, DrawElementsType.UnsignedInt, 0);
        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        GL.Enable(EnableCap.CullFace);
        GL.BindVertexArray(0);
    }

    private Vector3[] GenerateCubeVertices()
    {
        Vector3 minBounds = _bounds.Min;
        Vector3 maxBounds = _bounds.Max;

        Vector3[] vertices =
        [
            new Vector3(minBounds.X, minBounds.Y, minBounds.Z),
            new Vector3(maxBounds.X, minBounds.Y, minBounds.Z),
            new Vector3(maxBounds.X, maxBounds.Y, minBounds.Z),
            new Vector3(minBounds.X, maxBounds.Y, minBounds.Z),
            new Vector3(minBounds.X, minBounds.Y, maxBounds.Z),
            new Vector3(maxBounds.X, minBounds.Y, maxBounds.Z),
            new Vector3(maxBounds.X, maxBounds.Y, maxBounds.Z),
            new Vector3(minBounds.X, maxBounds.Y, maxBounds.Z)
        ];

        return vertices;
    }
}