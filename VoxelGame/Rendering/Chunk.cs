using OpenTK.Mathematics;
using VoxelGame.Maths;
using Random = VoxelGame.Maths.Random;
using Vector3 = VoxelGame.Maths.Vector3;

namespace VoxelGame.Rendering;

public sealed class Chunk
{
    public const int ChunkSize = 32;
    public const int ChunkArea = ChunkSize * ChunkSize;
    public const int ChunkVolume = ChunkArea * ChunkSize;
    
    public Vector3Int chunkPosition;

    private uint[] _voxels;

    private Shader _shader;
    private int _vao, _vbo, _ebo;

    private int _vertexCount;
    private int _triangleCount;

    private Matrix4 m_model;

    public Chunk(Vector3Int chunkPosition, Shader shader)
    {
        this.chunkPosition = chunkPosition;
        _shader = shader;

        _voxels = new uint[ChunkVolume];

        // TODO: Remove this line
        Array.Fill<uint>(_voxels, 1);
        // for (int i = 0; i < ChunkVolume; i++)
        // {
        //     _voxels[i] = Random.Value > 0.9f ? 1u : 0u;
        // }

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        _ebo = GL.GenBuffer();
        
        m_model = Matrix4.CreateTranslation(chunkPosition);
    }

    internal void BuildChunk()
    {
        List<Vector3> vertices = new();
        List<int> triangles = new();

        for (int i = 0, triangleIndex = 0; i < _voxels.Length; i++)
        {
            int x = i % ChunkSize;
            int y = (i / ChunkSize) % ChunkSize;
            int z = i / ChunkArea;
            Vector3Int voxelPosition = new(x, y, z);
            Vector3Int worldPosition = chunkPosition * ChunkSize + new Vector3Int(x, y, z);
            if (_voxels[i] != 0)
            {
                // Front, back, up, down, right, left
                bool[] sides =
                [
                    IsAir(voxelPosition + new Vector3Int(0, 0, -1), _voxels),
                    IsAir(voxelPosition + new Vector3Int(0, 0, 1), _voxels),
                    IsAir(voxelPosition + new Vector3Int(0, 1, 0), _voxels),
                    IsAir(voxelPosition + new Vector3Int(0, -1, 0), _voxels),
                    IsAir(voxelPosition + new Vector3Int(1, 0, 0), _voxels),
                    IsAir(voxelPosition + new Vector3Int(-1, 0, 0), _voxels),
                ];

                if (sides[0])
                {
                    // Front face
                    vertices.AddRange([
                        new(x - 0.5f, y - 0.5f, z - 0.5f),
                        new(x - 0.5f, y + 0.5f, z - 0.5f),
                        new(x + 0.5f, y - 0.5f, z - 0.5f),
                        new(x + 0.5f, y + 0.5f, z - 0.5f),
                    ]);
                    triangles.AddRange([
                        0 + triangleIndex, 1 + triangleIndex, 2 + triangleIndex,
                        2 + triangleIndex, 1 + triangleIndex, 3 + triangleIndex,
                    ]);
                    triangleIndex += 4;
                }

                if (sides[1])
                {
                    // Back face
                    vertices.AddRange([
                        new(x - 0.5f, y - 0.5f, z + 0.5f),
                        new(x - 0.5f, y + 0.5f, z + 0.5f),
                        new(x + 0.5f, y - 0.5f, z + 0.5f),
                        new(x + 0.5f, y + 0.5f, z + 0.5f),
                    ]);
                    triangles.AddRange([
                        0 + triangleIndex, 2 + triangleIndex, 1 + triangleIndex,
                        2 + triangleIndex, 3 + triangleIndex, 1 + triangleIndex,
                    ]);
                    triangleIndex += 4;
                }

                if (sides[2])
                {
                    // Top face
                    vertices.AddRange([
                        new(x - 0.5f, y + 0.5f, z - 0.5f),
                        new(x + 0.5f, y + 0.5f, z - 0.5f),
                        new(x - 0.5f, y + 0.5f, z + 0.5f),
                        new(x + 0.5f, y + 0.5f, z + 0.5f),
                    ]);
                    triangles.AddRange([
                        0 + triangleIndex, 2 + triangleIndex, 1 + triangleIndex,
                        2 + triangleIndex, 3 + triangleIndex, 1 + triangleIndex,
                    ]);
                    triangleIndex += 4;
                }

                if (sides[3])
                {
                    // Bottom face
                    vertices.AddRange([
                        new(x - 0.5f, y - 0.5f, z - 0.5f),
                        new(x + 0.5f, y - 0.5f, z - 0.5f),
                        new(x - 0.5f, y - 0.5f, z + 0.5f),
                        new(x + 0.5f, y - 0.5f, z + 0.5f),
                    ]);
                    triangles.AddRange([
                        0 + triangleIndex, 1 + triangleIndex, 2 + triangleIndex,
                        2 + triangleIndex, 1 + triangleIndex, 3 + triangleIndex,
                    ]);
                    triangleIndex += 4;
                }

                if (sides[4])
                {
                    // Right face
                    vertices.AddRange([
                        new(x + 0.5f, y - 0.5f, z - 0.5f),
                        new(x + 0.5f, y + 0.5f, z - 0.5f),
                        new(x + 0.5f, y - 0.5f, z + 0.5f),
                        new(x + 0.5f, y + 0.5f, z + 0.5f),
                    ]);
                    triangles.AddRange([
                        0 + triangleIndex, 1 + triangleIndex, 2 + triangleIndex,
                        2 + triangleIndex, 1 + triangleIndex, 3 + triangleIndex,
                    ]);
                    triangleIndex += 4;
                }

                if (sides[5])
                {
                    // Left face
                    vertices.AddRange([
                        new(x - 0.5f, y - 0.5f, z - 0.5f),
                        new(x - 0.5f, y + 0.5f, z - 0.5f),
                        new(x - 0.5f, y - 0.5f, z + 0.5f),
                        new(x - 0.5f, y + 0.5f, z + 0.5f),
                    ]);
                    triangles.AddRange([
                        0 + triangleIndex, 2 + triangleIndex, 1 + triangleIndex,
                        2 + triangleIndex, 3 + triangleIndex, 1 + triangleIndex,
                    ]);
                    triangleIndex += 4;
                }
            }
        }

        _triangleCount = triangles.Count;
        _vertexCount = vertices.Count;

        const uint stride = 3; // each vertex has 3 floats: 3 position
        float[] data = new float[vertices.Count * stride];

        for (int i = 0; i < vertices.Count; i++)
        {
            data[i * stride + 0] = vertices[i].X;
            data[i * stride + 1] = vertices[i].Y;
            data[i * stride + 2] = vertices[i].Z;
        }
        
        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float), data,
            BufferUsageHint.StaticDraw);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, triangles.Count * sizeof(int), triangles.ToArray(),
            BufferUsageHint.StaticDraw);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Vector3.Size, 0);
        GL.EnableVertexAttribArray(0);

        GL.BindVertexArray(0);
    }

    public static bool IsAir(Vector3Int voxelPos, uint[] voxels)
    {
        if (voxelPos.X >= 0 && voxelPos.X < ChunkSize &&
            voxelPos.Y >= 0 && voxelPos.Y < ChunkSize &&
            voxelPos.Z >= 0 && voxelPos.Z < ChunkSize)
        {
            int index = (voxelPos.Z * ChunkSize * ChunkSize) + (voxelPos.Y * ChunkSize) + voxelPos.X;
            return voxels[index] == 0;
        }
        else
        {
            return true;
        }
    }

    internal void Render(Camera camera)
    {
        _shader.SetUniform("m_proj", ref camera.ProjectionMatrix);
        _shader.SetUniform("m_view", ref camera.ViewMatrix);
        _shader.SetUniform("m_model", ref m_model);
        
        GL.BindVertexArray(_vao);
        _shader.Use();
        GL.DrawElements(PrimitiveType.Triangles, _triangleCount, DrawElementsType.UnsignedInt, 0);
        ErrorCode glError = GL.GetError();
        if (glError != ErrorCode.NoError)
        {
            Console.WriteLine(_vao);
            Console.WriteLine(_vbo);
            Console.WriteLine(_ebo);
            throw new GLException($"OpenGL error: {glError}");
        }
        GL.BindVertexArray(0);

        Engine.VertexCount = _vertexCount;
        Engine.TriangleCount = _triangleCount / 3;
    }
}

public class GLException : Exception
{
    public GLException(string message) : base(message)
    {
    }

    public GLException(ErrorCode code, string message) : base($"GL Error Code: {code}. {message}")
    {
    }
    
    public GLException(ErrorCode code) : base($"GL Error: {code}")
    {
    }
}