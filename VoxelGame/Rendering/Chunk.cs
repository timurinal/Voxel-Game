using System.Runtime.CompilerServices;
using OpenTK.Mathematics;
using SimplexNoise;
using VoxelGame.Maths;
using Random = VoxelGame.Maths.Random;
using Vector2 = VoxelGame.Maths.Vector2;
using Vector3 = VoxelGame.Maths.Vector3;

namespace VoxelGame.Rendering;

public sealed class Chunk
{
    public const int ChunkSize = 16;
    public const int ChunkArea = ChunkSize * ChunkSize;
    public const int ChunkVolume = ChunkArea * ChunkSize;
    
    public Vector3Int chunkPosition;
    
    public bool IsEmpty { get; private set; }
    
    public AABB Bounds { get; private set; }

    // if this is true, it means the chunk has been modified since generation
    public bool IsDirty { get; private set; } = false;
    
    public uint solidVoxelCount { get; private set; }

    public uint[] voxels { get; set; }

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

        for (int i = 0; i < ChunkVolume; i++)
        {
            int x = i % ChunkSize;
            int y = (i / ChunkSize) % ChunkSize;
            int z = i / ChunkArea;

            // Calculate the global y position
            int globalY = y + this.chunkPosition.Y;

            // Generate a flat plane only at the bottom of the world
            if (globalY <= 0)
                voxels[i] = 4u;
            else if (globalY <= 4)
                voxels[i] = 2u;
            else if (globalY <= 6)
                voxels[i] = 3u;
            else if (globalY <= 7)
                voxels[i] = 1u;
            else
                voxels[i] = 0u;
        }

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        _ebo = GL.GenBuffer();
        
        m_model = Matrix4.CreateTranslation(chunkPosition);
        
        Vector3 chunkCentre = chunkPosition + new Vector3(ChunkSize / 2f, ChunkSize / 2f, ChunkSize / 2f);

        // Create the AABB using the centre and half dimensions
        Bounds = new AABB(chunkCentre, ChunkSize / 2f, ChunkSize / 2f, ChunkSize / 2f);
    }

    internal void BuildChunk(Dictionary<Vector3Int, Chunk> chunks)
    {
        solidVoxelCount = 0;

        IsEmpty = true;
        foreach (var v in voxels)
            if (v != 0)
            {
                IsEmpty = false;
                break;
            }

        if (!IsEmpty)
        {
            List<Vector3> vertices = new();
            List<Vector3> normals = new();
            List<Vector2> uvs = new();
            List<int> faceIds = new(); // 0 = front, 1 = back, 2 = up, 3 = down, 4 = right, 5 = left
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
                    if (IsAir(voxelPosition.X, voxelPosition.Y, voxelPosition.Z - 1, voxels, chunks,
                            (Vector3Int)(chunkPosition / ChunkSize)))
                    {
                        // Front face
                        vertices.AddRange(new Vector3[]
                        {
                            new(x - 0.5f, y - 0.5f, z - 0.5f),
                            new(x - 0.5f, y + 0.5f, z - 0.5f),
                            new(x + 0.5f, y - 0.5f, z - 0.5f),
                            new(x + 0.5f, y + 0.5f, z - 0.5f),
                        });
                        for (int j = 0; j < 4; j++)
                            normals.Add(new Vector3(0, 0, -1));
                        uvs.AddRange(new[]
                        {
                            uv00, uv01, uv10, uv11
                        });
                        for (int j = 0; j < 4; j++)
                            faceIds.Add(0);
                        triangles.AddRange(new[]
                        {
                            0 + triangleIndex, 1 + triangleIndex, 2 + triangleIndex,
                            2 + triangleIndex, 1 + triangleIndex, 3 + triangleIndex,
                        });
                        triangleIndex += 4;
                    }
                    if (IsAir(voxelPosition.X, voxelPosition.Y, voxelPosition.Z + 1, voxels, chunks,
                        (Vector3Int)(chunkPosition / ChunkSize)))
                    {
                        // Back face
                        vertices.AddRange(new Vector3[]
                        {
                            new(x - 0.5f, y - 0.5f, z + 0.5f),
                            new(x - 0.5f, y + 0.5f, z + 0.5f),
                            new(x + 0.5f, y - 0.5f, z + 0.5f),
                            new(x + 0.5f, y + 0.5f, z + 0.5f),
                        });
                        for (int j = 0; j < 4; j++)
                            normals.Add(new Vector3(0, 0, 1));
                        uvs.AddRange(new[]
                        {
                            uv00, uv01, uv10, uv11
                        });
                        for (int j = 0; j < 4; j++)
                            faceIds.Add(1);
                        triangles.AddRange(new[]
                        {
                            0 + triangleIndex, 2 + triangleIndex, 1 + triangleIndex,
                            2 + triangleIndex, 3 + triangleIndex, 1 + triangleIndex,
                        });
                        triangleIndex += 4;
                    }
                    if (IsAir(voxelPosition.X, voxelPosition.Y + 1, voxelPosition.Z, voxels, chunks,
                            (Vector3Int)(chunkPosition / ChunkSize)))
                    {
                        // Top face
                        vertices.AddRange(new Vector3[]
                        {
                            new(x - 0.5f, y + 0.5f, z - 0.5f),
                            new(x + 0.5f, y + 0.5f, z - 0.5f),
                            new(x - 0.5f, y + 0.5f, z + 0.5f),
                            new(x + 0.5f, y + 0.5f, z + 0.5f),
                        });
                        for (int j = 0; j < 4; j++)
                            normals.Add(new Vector3(0, 1, 0));
                        uvs.AddRange(new[]
                        {
                            uv00, uv01, uv10, uv11
                        });
                        for (int j = 0; j < 4; j++)
                            faceIds.Add(2);
                        triangles.AddRange(new[]
                        {
                            0 + triangleIndex, 2 + triangleIndex, 1 + triangleIndex,
                            2 + triangleIndex, 3 + triangleIndex, 1 + triangleIndex,
                        });
                        triangleIndex += 4;
                    }
                    if (IsAir(voxelPosition.X, voxelPosition.Y - 1, voxelPosition.Z, voxels, chunks,
                            (Vector3Int)(chunkPosition / ChunkSize)))
                    {
                        // Bottom face
                        vertices.AddRange(new Vector3[]
                        {
                            new(x - 0.5f, y - 0.5f, z - 0.5f),
                            new(x + 0.5f, y - 0.5f, z - 0.5f),
                            new(x - 0.5f, y - 0.5f, z + 0.5f),
                            new(x + 0.5f, y - 0.5f, z + 0.5f),
                        });
                        for (int j = 0; j < 4; j++)
                            normals.Add(new Vector3(0, -1, 0));
                        uvs.AddRange(new[]
                        {
                            uv00, uv01, uv10, uv11
                        });
                        for (int j = 0; j < 4; j++)
                            faceIds.Add(3);
                        triangles.AddRange(new[]
                        {
                            0 + triangleIndex, 1 + triangleIndex, 2 + triangleIndex,
                            2 + triangleIndex, 1 + triangleIndex, 3 + triangleIndex,
                        });
                        triangleIndex += 4;
                    }
                    if (IsAir(voxelPosition.X + 1, voxelPosition.Y, voxelPosition.Z, voxels, chunks,
                            (Vector3Int)(chunkPosition / ChunkSize)))
                    {
                        // Right face
                        vertices.AddRange(new Vector3[]
                        {
                            new(x + 0.5f, y - 0.5f, z - 0.5f),
                            new(x + 0.5f, y + 0.5f, z - 0.5f),
                            new(x + 0.5f, y - 0.5f, z + 0.5f),
                            new(x + 0.5f, y + 0.5f, z + 0.5f),
                        });
                        for (int j = 0; j < 4; j++)
                            normals.Add(new Vector3(1, 0, 0));
                        uvs.AddRange(new[]
                        {
                            uv00, uv01, uv10, uv11
                        });
                        for (int j = 0; j < 4; j++)
                            faceIds.Add(4);
                        triangles.AddRange(new[]
                        {
                            0 + triangleIndex, 1 + triangleIndex, 2 + triangleIndex,
                            2 + triangleIndex, 1 + triangleIndex, 3 + triangleIndex,
                        });
                        triangleIndex += 4;
                    }
                    if (IsAir(voxelPosition.X - 1, voxelPosition.Y, voxelPosition.Z, voxels, chunks,
                            (Vector3Int)(chunkPosition / ChunkSize)))
                    {
                        // Left face
                        vertices.AddRange(new Vector3[]
                        {
                            new(x - 0.5f, y - 0.5f, z - 0.5f),
                            new(x - 0.5f, y + 0.5f, z - 0.5f),
                            new(x - 0.5f, y - 0.5f, z + 0.5f),
                            new(x - 0.5f, y + 0.5f, z + 0.5f),
                        });
                        for (int j = 0; j < 4; j++)
                            normals.Add(new Vector3(-1, 0, 1));
                        uvs.AddRange(new[]
                        {
                            uv00, uv01, uv10, uv11
                        });
                        for (int j = 0; j < 4; j++)
                            faceIds.Add(5);
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

            const int stride = 9; // each vertex has 3 position floats, 3 normal floats, 2 UV floats, and 1 faceid integer
            float[] data = new float[vertices.Count * stride];
            
            for (int i = 0; i < vertices.Count; i++)
            {
                data[i * stride + 0] = vertices[i].X;
                data[i * stride + 1] = vertices[i].Y;
                data[i * stride + 2] = vertices[i].Z;
                
                data[i * stride + 3] = normals[i].X;
                data[i * stride + 4] = normals[i].Y;
                data[i * stride + 5] = normals[i].Z;
                
                data[i * stride + 6] = uvs[i].X;
                data[i * stride + 7] = uvs[i].Y;
                
                data[i * stride + 8] = faceIds[i];
            }

            GL.BindVertexArray(_vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float), data, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, triangles.Count * sizeof(int), triangles.ToArray(), BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride * sizeof(float), 0 * sizeof(float));
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride * sizeof(float), 3 * sizeof(float));
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride * sizeof(float), 6 * sizeof(float));
            GL.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, stride * sizeof(float), 8 * sizeof(float));
            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);
            GL.EnableVertexAttribArray(3);

            GL.BindVertexArray(0);
        }
    }
    
    internal void RebuildChunk(Dictionary<Vector3Int, Chunk> chunks, bool recursive = false)
    {
        BuildChunk(chunks);

        Vector3Int[] neighbourChunkOffsets =
        [
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
        ];

        if (recursive)
        {
            foreach (var offset in neighbourChunkOffsets)
            {
                if (chunks.TryGetValue(chunkPosition + offset, out var neighbour))
                {
                    neighbour.RebuildChunk(chunks);
                }
            }
        }
        
        IsDirty = true;
    }
    
    // private async Task<(float[] vertices, int[] triangles)> BuildChunkAsync(Dictionary<Vector3Int, Chunk> chunks)
    // {
    //     return await Task.Run(() =>
    //     {
    //         List<Vector3> vertexList = new();
    //         List<Vector2> uvList = new();
    //         List<int> faceIdList = new(); // 0 = up, 1 = down, 2 = front, 3 = back, 4 = right, 5 = left
    //         List<int> triangleList = new();
    //
    //         solidVoxelCount = 0;
    //
    //         for (int i = 0, triangleIndex = 0; i < voxels.Length; i++)
    //         {
    //             int x = i % ChunkSize;
    //             int y = (i / ChunkSize) % ChunkSize;
    //             int z = i / ChunkArea;
    //             Vector3Int voxelPosition = new(x, y, z);
    //             Vector3Int worldPosition = chunkPosition * ChunkSize + new Vector3Int(x, y, z);
    //
    //             if (voxels[i] != 0)
    //             {
    //                 solidVoxelCount++;
    //
    //                 int voxelID = (int)voxels[i];
    //                 Vector2 uv00 = GetUVForVoxel(voxelID - 1, 1, 1);
    //                 Vector2 uv01 = GetUVForVoxel(voxelID - 1, 1, 0);
    //                 Vector2 uv10 = GetUVForVoxel(voxelID - 1, 0, 1);
    //                 Vector2 uv11 = GetUVForVoxel(voxelID - 1, 0, 0);
    //
    //                 if (IsAir(voxelPosition + new Vector3Int(0, 0, -1), voxels, chunks))
    //                 {
    //                     // Front face
    //                     vertexList.AddRange(new Vector3[]
    //                     {
    //                         new(x - 0.5f, y - 0.5f, z - 0.5f),
    //                         new(x - 0.5f, y + 0.5f, z - 0.5f),
    //                         new(x + 0.5f, y - 0.5f, z - 0.5f),
    //                         new(x + 0.5f, y + 0.5f, z - 0.5f),
    //                     });
    //                     uvList.AddRange(new[]
    //                     {
    //                         uv00, uv01, uv10, uv11
    //                     });
    //                     for (int j = 0; j < 4; j++)
    //                         faceIdList.Add(0);
    //                     triangleList.AddRange(new[]
    //                     {
    //                         0 + triangleIndex, 1 + triangleIndex, 2 + triangleIndex,
    //                         2 + triangleIndex, 1 + triangleIndex, 3 + triangleIndex,
    //                     });
    //                     triangleIndex += 4;
    //                 }
    //
    //                 // Repeat for other faces...
    //             }
    //         }
    //
    //         const int stride = 6;
    //         float[] data = new float[vertexList.Count * stride];
    //         for (int i = 0; i < vertexList.Count; i++)
    //         {
    //             data[i * stride + 0] = vertexList[i].X;
    //             data[i * stride + 1] = vertexList[i].Y;
    //             data[i * stride + 2] = vertexList[i].Z;
    //             data[i * stride + 3] = uvList[i].X;
    //             data[i * stride + 4] = uvList[i].Y;
    //             data[i * stride + 5] = faceIdList[i];
    //         }
    //
    //         return (data, triangleList.ToArray());
    //     });
    // }
    private void SetupGLBuffers(float[] vertices, int[] triangles)
    {
        _triangleCount = triangles.Length;
        _vertexCount = vertices.Length / 6;

        const int stride = 6; // each vertex has 3 position floats, 2 UV floats, and 1 faceid integer

        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, triangles.Length * sizeof(int), triangles, BufferUsageHint.StaticDraw);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride * sizeof(float), 0 * sizeof(float));
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride * sizeof(float), 3 * sizeof(float));
        GL.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, stride * sizeof(float), 5 * sizeof(float));
        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(1);
        GL.EnableVertexAttribArray(2);

        GL.BindVertexArray(0);
    }
    
    // internal async Task CreateChunkAsync(Dictionary<Vector3Int, Chunk> chunks)
    // {
    //     var (vertices, triangles) = await BuildChunkAsync(chunks);
    //
    //     // OpenGL setup should be done on the main thread
    //     SetupGLBuffers(vertices, triangles);
    // }

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

    public static bool IsAir(int x, int y, int z, uint[] voxels, Dictionary<Vector3Int, Chunk> chunks, Vector3Int currentChunkPosition)
    {
        // Check if the coordinates are outside of the current chunk boundaries
        if (x < 0 || y < 0 || z < 0 || x >= ChunkSize || y >= ChunkSize || z >= ChunkSize)
        {
            // Calculate which chunk the voxel would be in
            Vector3Int neighborChunkPosition = currentChunkPosition + new Vector3Int(
                x < 0 ? -1 : (x >= ChunkSize ? 1 : 0),
                y < 0 ? -1 : (y >= ChunkSize ? 1 : 0),
                z < 0 ? -1 : (z >= ChunkSize ? 1 : 0)
            );

            // Try to get the neighbouring chunk
            if (chunks.TryGetValue(neighborChunkPosition, out Chunk neighborChunk))
            {
                // Convert global coordinates to local coordinates within the neighbouring chunk
                int localX = (x + ChunkSize) % ChunkSize;
                int localY = (y + ChunkSize) % ChunkSize;
                int localZ = (z + ChunkSize) % ChunkSize;
                int index = FlattenIndex3D(localX, localY, localZ, ChunkSize, ChunkSize);
                return neighborChunk.voxels[index] == 0;
            }
            else
            {
                return y <= 0;
            }
        }
        else
        {
            // Inside current chunk
            int index = FlattenIndex3D(x, y, z, ChunkSize, ChunkSize);
            return voxels[index] == 0;
        }
    }
    
    public static int FlattenIndex3D(int x, int y, int z, int width, int height)
    {
        return x + (y * width) + (z * width * height);
    }

    public static Vector3 UnflattenIndex1D(int index, int width, int height)
    {
        int z = index / (width * height);
        index -= (z * width * height);
        int y = index / width;
        int x = index % width;
        return new Vector3(x, y, z);
    }

    internal AABB[] GenerateCollisions()
    {
        List<AABB> aabbs = new();
        
        for (int i = 0; i < voxels.Length; i++)
        {
            if (voxels[i] == 0) continue;
            
            int x = i % ChunkSize;
            int y = (i / ChunkSize) % ChunkSize;
            int z = i / ChunkArea;
            Vector3Int worldPosition = chunkPosition + new Vector3Int(x, y, z);

            aabbs.Add(AABB.CreateVoxelAABB(worldPosition));
        }

        return aabbs.ToArray();
    }

    internal (int vertexCount, int triangleCount) Render(Player player, ShadowMapper shadowMapper)
    {
        if (IsEmpty)
            return (0, 0);
        
        _shader.Use();
        _shader.SetUniform("m_proj", ref player.ProjectionMatrix, autoUse: false);
        _shader.SetUniform("m_view", ref player.ViewMatrix, autoUse: false);
        _shader.SetUniform("m_model", ref m_model, autoUse: false);
        _shader.SetUniform("m_lightOrtho", ref shadowMapper.OrthographicMatrix, autoUse: false);
        _shader.SetUniform("m_lightView", ref shadowMapper.ViewMatrix, autoUse: false);
        _shader.SetUniform("shadowMap", 2, autoUse: false);
        
        GL.BindVertexArray(_vao);
        TextureAtlas.AlbedoTexture.Use(TextureUnit.Texture0);
        TextureAtlas.SpecularTexture.Use(TextureUnit.Texture1);
        GL.ActiveTexture(TextureUnit.Texture2);
        GL.BindTexture(TextureTarget.Texture2D, shadowMapper.DepthMap);
        GL.DrawElements(PrimitiveType.Triangles, _triangleCount, DrawElementsType.UnsignedInt, 0);
        ErrorCode glError = GL.GetError();
        if (glError != ErrorCode.NoError)
        {
            throw new GLException($"OpenGL Error: {glError}");
        }
        GL.BindVertexArray(0);

        return (_vertexCount, _triangleCount / 3);
    }
    public (int vertexCount, int triangleCount) Render(Matrix4 m_proj, Matrix4 m_view, Shader shaderOverride,
        bool overrideShader = false)
    {
        if (IsEmpty)
            return (0, 0);
        
        if (!overrideShader)
        {
            _shader.Use();
            _shader.SetUniform("m_proj", ref m_proj, autoUse: false);
            _shader.SetUniform("m_view", ref m_view, autoUse: false);
            _shader.SetUniform("m_model", ref m_model, autoUse: false);
        }
        else
        {
            shaderOverride.Use();
            shaderOverride.SetUniform("m_proj", ref m_proj, autoUse: false);
            shaderOverride.SetUniform("m_view", ref m_view, autoUse: false);
            shaderOverride.SetUniform("m_model", ref m_model, autoUse: false);
        }
        
        GL.BindVertexArray(_vao);
        TextureAtlas.AlbedoTexture.Use(TextureUnit.Texture0);
        TextureAtlas.SpecularTexture.Use(TextureUnit.Texture1);
        GL.DrawElements(PrimitiveType.Triangles, _triangleCount, DrawElementsType.UnsignedInt, 0);
        ErrorCode glError = GL.GetError();
        if (glError != ErrorCode.NoError)
        {
            throw new GLException($"OpenGL Error: {glError}");
        }
        GL.BindVertexArray(0);

        return (_vertexCount, _triangleCount / 3);
    }

    internal void OnSaved() => IsDirty = false;
}