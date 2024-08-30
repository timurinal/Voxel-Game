namespace VoxelGame.Graphics;

public class DeferredPreprocessor
{
    private int _vao, _vbo, _ebo;
    
    public DeferredPreprocessor()
    {
        float[] data =
        [
            // Position             // Uvs
            -1.0f, -1.0f, 0.0f,     0.0f, 0.0f,
            -1.0f,  1.0f, 0.0f,     0.0f, 1.0f,
             1.0f, -1.0f, 0.0f,     1.0f, 0.0f,
             1.0f,  1.0f, 0.0f,     1.0f, 1.0f,
        ];
        int[] triangles =
        [
            0, 1, 2,
            2, 1, 3,
        ]; 
        
        _vao = GL.GenVertexArray();
        GL.BindVertexArray(_vao);
        
        _vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float), data, BufferUsageHint.StaticDraw);
        
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0 * sizeof(float));
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(1);
        
        _ebo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, triangles.Length * sizeof(int), triangles, BufferUsageHint.StaticDraw);
        
        GL.BindVertexArray(0);
    }
}