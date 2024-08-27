namespace VoxelGame.Graphics.Shaders;

public struct ShaderStorageBuffer : IDisposable
{
    private int _buf;

    public ShaderStorageBuffer(int bufferBase)
    {
        _buf = GL.GenBuffer();
        Use();
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, bufferBase, _buf);
    }

    public void Use()
    {
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _buf);
    }

    public void SetData<T>(int stride, T[] data) where T : struct
    {
        Use();

        GL.BufferData(BufferTarget.ShaderStorageBuffer, stride * data.Length, data, BufferUsageHint.DynamicDraw);
    }

    public void Dispose()
    {
        GL.DeleteBuffer(_buf);
    }
}