using OpenTK.Mathematics;
using VoxelGame.Maths;
using Random = VoxelGame.Maths.Random;
using Vector2 = VoxelGame.Maths.Vector2;
using Vector3 = VoxelGame.Maths.Vector3;

namespace VoxelGame.Rendering;

public sealed class Chunk
{
    public const int ChunkSize = 8;
    public const int ChunkArea = ChunkSize * ChunkSize;
    public const int ChunkVolume = ChunkArea * ChunkSize;
    
    public Vector3Int chunkPosition;
    
    public uint solidVoxelCount { get; private set; }

    public uint[] voxels { get; private set; }

    private Shader _shader;
    private int _vao, _vbo, _ebo;

    private int _vertexCount;
    private int _triangleCount;

    private Matrix4 m_model;

    public Chunk(Vector3Int chunkPosition, Shader shader)
    {
        this.chunkPosition = chunkPosition;
        _shader = shader;

        voxels = new uint[ChunkVolume];

        // TODO: Remove this line
        // Array.Fill<uint>(_voxels, 1);
        for (int i = 0; i < ChunkVolume; i++)
        {
            voxels[i] = Random.Value >= 0.5f ? 1u : 2u;
        }

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        _ebo = GL.GenBuffer();
        
        m_model = Matrix4.CreateTranslation(chunkPosition);
    }

    internal void BuildChunk(Dictionary<Vector3Int, Chunk> chunks)
    {
        solidVoxelCount = 0;
        
        List<Vector3> vertices = new();
        List<Vector2> uvs = new();
        List<int> triangles = new();

        for (int i = 0, triangleIndex = 0; i < voxels.Length; i++)
        {
            int x = i % ChunkSize;
            int y = (i / ChunkSize) % ChunkSize;
            int z = i / ChunkArea;
            Vector3Int voxelPosition = new(x, y, z);
            Vector3Int worldPosition = chunkPosition * ChunkSize + new Vector3Int(x, y, z);
            if (voxels[i] != 0)
            {
                solidVoxelCount++;
                
                int voxelID = (int)voxels[i];
                Vector2 uv00 = GetUVForVoxel(voxelID - 1, 1, 1);
                Vector2 uv01 = GetUVForVoxel(voxelID - 1, 1, 0);
                Vector2 uv10 = GetUVForVoxel(voxelID - 1, 0, 1);
                Vector2 uv11 = GetUVForVoxel(voxelID - 1, 0, 0);

                // TODO: Cull faces between chunks
                if (IsAir(voxelPosition + new Vector3Int(0, 0, -1), voxels, chunks))
                {
                    // Front face
                    vertices.AddRange(new Vector3[]
                    {
                        new(x - 0.5f, y - 0.5f, z - 0.5f),
                        new(x - 0.5f, y + 0.5f, z - 0.5f),
                        new(x + 0.5f, y - 0.5f, z - 0.5f),
                        new(x + 0.5f, y + 0.5f, z - 0.5f),
                    });
                    uvs.AddRange(new[]
                    {
                        uv00, uv01, uv10, uv11
                    });
                    triangles.AddRange(new[]
                    {
                        0 + triangleIndex, 1 + triangleIndex, 2 + triangleIndex,
                        2 + triangleIndex, 1 + triangleIndex, 3 + triangleIndex,
                    });
                    triangleIndex += 4;
                }
                if (IsAir(voxelPosition + new Vector3Int(0, 0, 1), voxels, chunks))
                {
                    // Back face
                    vertices.AddRange(new Vector3[]
                    {
                        new(x - 0.5f, y - 0.5f, z + 0.5f),
                        new(x - 0.5f, y + 0.5f, z + 0.5f),
                        new(x + 0.5f, y - 0.5f, z + 0.5f),
                        new(x + 0.5f, y + 0.5f, z + 0.5f),
                    });
                    uvs.AddRange(new[]
                    {
                        uv00, uv01, uv10, uv11
                    });
                    triangles.AddRange(new[]
                    {
                        0 + triangleIndex, 2 + triangleIndex, 1 + triangleIndex,
                        2 + triangleIndex, 3 + triangleIndex, 1 + triangleIndex,
                    });
                    triangleIndex += 4;
                }
                if (IsAir(voxelPosition + new Vector3Int(0, 1, 0), voxels, chunks))
                {
                    // Top face
                    vertices.AddRange(new Vector3[]
                    {
                        new(x - 0.5f, y + 0.5f, z - 0.5f),
                        new(x + 0.5f, y + 0.5f, z - 0.5f),
                        new(x - 0.5f, y + 0.5f, z + 0.5f),
                        new(x + 0.5f, y + 0.5f, z + 0.5f),
                    });
                    uvs.AddRange(new[]
                    {
                        uv00, uv01, uv10, uv11
                    });
                    triangles.AddRange(new[]
                    {
                        0 + triangleIndex, 2 + triangleIndex, 1 + triangleIndex,
                        2 + triangleIndex, 3 + triangleIndex, 1 + triangleIndex,
                    });
                    triangleIndex += 4;
                }
                if (IsAir(voxelPosition + new Vector3Int(0, -1, 0), voxels, chunks))
                {
                    // Bottom face
                    vertices.AddRange(new Vector3[]
                    {
                        new(x - 0.5f, y - 0.5f, z - 0.5f),
                        new(x + 0.5f, y - 0.5f, z - 0.5f),
                        new(x - 0.5f, y - 0.5f, z + 0.5f),
                        new(x + 0.5f, y - 0.5f, z + 0.5f),
                    });
                    uvs.AddRange(new[]
                    {
                        uv00, uv01, uv10, uv11
                    });
                    triangles.AddRange(new[]
                    {
                        0 + triangleIndex, 1 + triangleIndex, 2 + triangleIndex,
                        2 + triangleIndex, 1 + triangleIndex, 3 + triangleIndex,
                    });
                    triangleIndex += 4;
                }
                if (IsAir(voxelPosition + new Vector3Int(1, 0, 0), voxels, chunks))
                {
                    // Right face
                    vertices.AddRange(new Vector3[]
                    {
                        new(x + 0.5f, y - 0.5f, z - 0.5f),
                        new(x + 0.5f, y + 0.5f, z - 0.5f),
                        new(x + 0.5f, y - 0.5f, z + 0.5f),
                        new(x + 0.5f, y + 0.5f, z + 0.5f),
                    });
                    uvs.AddRange(new[]
                    {
                        uv00, uv01, uv10, uv11
                    });
                    triangles.AddRange(new[]
                    {
                        0 + triangleIndex, 1 + triangleIndex, 2 + triangleIndex,
                        2 + triangleIndex, 1 + triangleIndex, 3 + triangleIndex,
                    });
                    triangleIndex += 4;
                }
                if (IsAir(voxelPosition + new Vector3Int(-1, 0, 0), voxels, chunks))
                {
                    // Left face
                    vertices.AddRange(new Vector3[]
                    {
                        new(x - 0.5f, y - 0.5f, z - 0.5f),
                        new(x - 0.5f, y + 0.5f, z - 0.5f),
                        new(x - 0.5f, y - 0.5f, z + 0.5f),
                        new(x - 0.5f, y + 0.5f, z + 0.5f),
                    });
                    uvs.AddRange(new[]
                    {
                        uv00, uv01, uv10, uv11
                    });
                    triangles.AddRange(new[]
                    {
                        0 + triangleIndex, 2 + triangleIndex, 1 + triangleIndex,
                        2 + triangleIndex, 3 + triangleIndex, 1 + triangleIndex,
                    });
                    triangleIndex += 4;
                }
            }
        }

        _triangleCount = triangles.Count;
        _vertexCount = vertices.Count;

        const int stride = 5; // each vertex has 3 position floats and 2 UV floats
        float[] data = new float[vertices.Count * stride];

        for (int i = 0; i < vertices.Count; i++)
        {
            data[i * stride + 0] = vertices[i].X;
            data[i * stride + 1] = vertices[i].Y;
            data[i * stride + 2] = vertices[i].Z;
            data[i * stride + 3] = uvs[i].X;
            data[i * stride + 4] = uvs[i].Y;
        }

        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float), data, BufferUsageHint.StaticDraw);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, triangles.Count * sizeof(int), triangles.ToArray(), BufferUsageHint.StaticDraw);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        GL.BindVertexArray(0);
    }

    private Vector2 GetUVForVoxel(int voxelID, int u, int v)
    {
        int texturePerRow = TextureAtlas.AtlasWidth / TextureAtlas.VoxelTextureSize;
        float unit = 1.0f / texturePerRow;

        // Padding to avoid bleeding
        float padding = 0.001f;

        float x = (voxelID % texturePerRow) * unit + padding;
        float y = (voxelID / texturePerRow) * unit + padding;
        float adjustedUnit = unit - 2 * padding;

        return new Vector2(x + u * adjustedUnit, y + v * adjustedUnit);
    }

    private static bool IsAir(Vector3Int voxelPos, uint[] voxels, Dictionary<Vector3Int, Chunk> chunks)
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

    internal AABB[] GenerateCollisions()
    {
        var aabbs = new AABB[solidVoxelCount];
        
        for (int i = 0; i < voxels.Length; i++)
        {
            if (voxels[i] == 0) continue;
            
            int x = i % ChunkSize;
            int y = (i / ChunkSize) % ChunkSize;
            int z = i / ChunkArea;
            Vector3Int worldPosition = chunkPosition + new Vector3Int(x, y, z);

            aabbs[i] = AABB.CreateVoxelAABB(worldPosition);
        }

        return aabbs;
    }

    internal void Render(Player player)
    {
        _shader.SetUniform("m_proj", ref player.ProjectionMatrix);
        _shader.SetUniform("m_view", ref player.ViewMatrix);
        _shader.SetUniform("m_model", ref m_model);
        
        GL.BindVertexArray(_vao);
        _shader.Use();
        TextureAtlas.AtlasTexture.Use();
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

        Engine.VertexCount += _vertexCount;
        Engine.TriangleCount += _triangleCount / 3;
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