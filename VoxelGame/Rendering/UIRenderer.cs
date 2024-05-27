using VoxelGame.Maths;

namespace VoxelGame.Rendering;

public class UIRenderer
{
    private int[] _vaos;
    
    internal UIRenderer()
    {
        
    }

    struct UIElement : IRenderable
    {
        public Vector3[] vertices;
        public int[] triangles;

        private float[] data;

        public int vao, vbo, ebo;

        public UIElement(Vector3[] vertices, int[] triangles)
        {
            this.vertices = vertices;
            this.triangles = triangles;

            vao = GL.GenVertexArray();
        }

        public (int vertexCount, int triangleCount) Render(Player player)
        {
            throw new NotImplementedException();
        }
    }
}