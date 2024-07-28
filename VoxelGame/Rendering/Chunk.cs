using OpenTK.Mathematics;
using VoxelGame.Maths;
using VoxelGame.TerrainGeneration;
using VoxelGame.Threading;
using Vector2 = VoxelGame.Maths.Vector2;
using Vector3 = VoxelGame.Maths.Vector3;
using Vector4 = OpenTK.Mathematics.Vector4;

namespace VoxelGame.Rendering;

public sealed class Chunk
{
    public const int ChunkSize = 32;
    public const int HChunkSize = ChunkSize / 2;
    public const int ChunkArea = ChunkSize * ChunkSize;
    public const int ChunkVolume = ChunkArea * ChunkSize;
    public static readonly float ChunkSphereRadius = ChunkSize * Mathf.Sqrt(3) / 2f;
    
    const int Stride = 8; // each vertex has 3 position floats, 3 normal floats, 2 UV floats
    
    public const int MaxStackSize = 250_000;
    
    public Vector3Int chunkPosition;
    public Vector3 chunkCentre;

    public Vector3Int ChunkSpacePosition => Vector3.Round(chunkPosition / ChunkSize);

    public Vector3 Center =>
        new(chunkPosition.X + ChunkSize / 2f, chunkPosition.Y + ChunkSize / 2f,
            chunkPosition.Z + ChunkSize / 2f);

    public AABB Bounds;

    public bool IsEmpty { get; private set; }

    // if this is true, it means the chunk has been modified since generation (or since it was last loaded from the disk)
    public bool IsDirty { get; private set; } = false;

    // This is true when the chunk is fully ready to be rendered by OpenGL
    private bool IsRenderReady = false;

    private bool ContainsTransparentVoxels = false;
    
    public uint solidVoxelCount { get; private set; }

    public uint[] voxels { get; set; }

    private Shader _shader;
    private int _vao, _vbo, _ebo;
    
    private Shader _transparentShader;
    private int _transparentVao, _transparentVbo, _transparentEbo;

    private Vector3[] _transparentVertices;
    private int[] _transparentTriangles;

    private int _vertexCount;
    private int _triangleCount, _transparentTriangleCount;

    private Matrix4 m_model;

    public Chunk(Vector3Int chunkPosition, Shader shader, Shader transparentShader)
    {
        this.chunkPosition = chunkPosition;
        _shader = shader;

        _transparentShader = transparentShader;

        voxels = new uint[ChunkVolume];

        // Array.Fill<uint>(voxels, 1);

        Parallel.For(0, ChunkVolume, i =>
        {
            int x = i % ChunkSize;
            int y = (i / ChunkSize) % ChunkSize;
            int z = i / ChunkArea;
        
            // Calculate the global y position
            int globalX = x + this.chunkPosition.X;
            int globalY = y + this.chunkPosition.Y;
            int globalZ = z + this.chunkPosition.Z;
        
            // voxels[i] = TerrainGenerator.Sample(globalX, globalY, globalZ) > 0.5f ? 2u : 0u;
        
            voxels[i] = TerrainGenerator.SampleTerrain(globalX, globalY, globalZ);
        });

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        _ebo = GL.GenBuffer();
        
        _transparentVao = GL.GenVertexArray();
        _transparentVbo = GL.GenBuffer();
        _transparentEbo = GL.GenBuffer();
        
        m_model = Matrix4.CreateTranslation(chunkPosition);
        
        chunkCentre = chunkPosition + new Vector3(ChunkSize / 2f, ChunkSize / 2f, ChunkSize / 2f);

        Bounds = AABB.CreateFromExtents(chunkCentre, new Vector3(ChunkSize / 2f, ChunkSize / 2f, ChunkSize / 2f));
    }

    internal async void BuildChunk(Dictionary<Vector3Int, Chunk> chunks, Vector3 cameraPos)
    {
        solidVoxelCount = 0;

        IsEmpty = true;
        foreach (var v in voxels)
            if (v != 0)
            {
                IsEmpty = false;
                break;
            }

        var result = await Task.Run(() => BuildChunkAsync(chunks, cameraPos));
        if (result.hasData)
        {
            Engine.RegisterMainThreadAction(() =>
            {
                GL.BindVertexArray(_vao);
                GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
                GL.BufferData(BufferTarget.ArrayBuffer, result.data.Length * sizeof(float), result.data, BufferUsageHint.StaticDraw);

                GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
                GL.BufferData(BufferTarget.ElementArrayBuffer, result.triangles.Length * sizeof(int), result.triangles, BufferUsageHint.StaticDraw);

                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Stride * sizeof(float), 0 * sizeof(float));
                GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, Stride * sizeof(float), 3 * sizeof(float));
                GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, Stride * sizeof(float), 6 * sizeof(float));
                GL.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, Stride * sizeof(float), 8 * sizeof(float));
                GL.EnableVertexAttribArray(0);
                GL.EnableVertexAttribArray(1);
                GL.EnableVertexAttribArray(2);
                GL.EnableVertexAttribArray(3);

                GL.BindVertexArray(0);

                if (result.containsTransparentVoxels)
                {
                    GL.BindVertexArray(_transparentVao);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, _transparentVbo);
                    GL.BufferData(BufferTarget.ArrayBuffer, result.transparentData.Length * sizeof(float), result.transparentData, BufferUsageHint.StaticDraw);

                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, _transparentEbo);
                    GL.BufferData(BufferTarget.ElementArrayBuffer, result.transparentTriangles.Length * sizeof(int), result.transparentTriangles, BufferUsageHint.StaticDraw);

                    GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Stride * sizeof(float), 0 * sizeof(float));
                    GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, Stride * sizeof(float), 3 * sizeof(float));
                    GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, Stride * sizeof(float), 6 * sizeof(float));
                    GL.EnableVertexAttribArray(0);
                    GL.EnableVertexAttribArray(1);
                    GL.EnableVertexAttribArray(2);

                    GL.BindVertexArray(0);

                    _transparentVertices = result.transparentVertices;
                    _transparentTriangles = result.transparentTriangles;
                }
                
                ContainsTransparentVoxels = result.containsTransparentVoxels;

                IsRenderReady = true;
                
                Engine.CheckGLError("End of chunk generation");
            });
        }
    }
    
    private (bool hasData, 
        float[] data, 
        int[] triangles, 
        bool containsTransparentVoxels, 
        Vector3[] transparentVertices, 
        float[] transparentData, int[] 
        transparentTriangles) BuildChunkAsync(Dictionary<Vector3Int, Chunk> chunks, Vector3 cameraPos)
    {
        if (!IsEmpty)
        {
            List<Vector3> vertices = new();
            List<Vector3> normals = new();
            List<Vector2> uvs = new();
            List<int> triangles = new();
            
            List<Vector3> transparentVertices = new();
            List<Vector3> transparentNormals = new();
            List<Vector2> transparentUvs = new();
            List<int> transparentTriangles = new();
            
            bool containsTransparentVoxels = false;

            for (int i = 0, triangleIndex = 0, transparentTriangleIndex = 0; i < voxels.Length; i++)
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
                    uint uvoxelID = voxels[i];

                    bool isTransparent = VoxelData.IsTransparent(uvoxelID);

                    containsTransparentVoxels |= isTransparent;

                    const float liquidHeight = 0.4375f;

                    if (!isTransparent)
                    {
                        // Front face
                        if (IsTransparent(voxelPosition.X, voxelPosition.Y, voxelPosition.Z - 1, voxels, chunks,
                                (Vector3Int)(chunkPosition / ChunkSize), voxels[i]))
                        {
                            Vector2 uv00 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Front, 1, 1);
                            Vector2 uv01 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Front, 1, 0);
                            Vector2 uv10 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Front, 0, 1);
                            Vector2 uv11 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Front, 0, 0);

                            if (VoxelData.IsLiquid(uvoxelID))
                            {
                                uint aboveVoxelId =
                                    Engine.GetVoxelAtPosition(voxelPosition + new Vector3Int(0, 1, 0)).voxelId ?? 0;
                                bool isLiquidAbove = VoxelData.IsLiquid(aboveVoxelId);

                                if (!isLiquidAbove)
                                {
                                    vertices.AddRange(new Vector3[]
                                    {
                                        new(x - 0.5f, y - 0.5f, z - 0.5f),
                                        new(x - 0.5f, y + 0.5f, z - 0.5f),
                                        new(x + 0.5f, y - 0.5f, z - 0.5f),
                                        new(x + 0.5f, y + 0.5f, z - 0.5f),
                                    });
                                }
                                else
                                {
                                    vertices.AddRange(new Vector3[]
                                    {
                                        new(x - 0.5f, y - 0.5f, z - 0.5f),
                                        new(x - 0.5f, y + liquidHeight, z - 0.5f),
                                        new(x + 0.5f, y - 0.5f, z - 0.5f),
                                        new(x + 0.5f, y + liquidHeight, z - 0.5f),
                                    });
                                }
                            }
                            else
                            {
                                vertices.AddRange(new Vector3[]
                                {
                                    new(x - 0.5f, y - 0.5f, z - 0.5f),
                                    new(x - 0.5f, y + 0.5f, z - 0.5f),
                                    new(x + 0.5f, y - 0.5f, z - 0.5f),
                                    new(x + 0.5f, y + 0.5f, z - 0.5f),
                                });
                            }
                            
                            for (int j = 0; j < 4; j++)
                                normals.Add(new Vector3(0, 0, -1));
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

                        // Back face
                        if (IsTransparent(voxelPosition.X, voxelPosition.Y, voxelPosition.Z + 1, voxels, chunks,
                                (Vector3Int)(chunkPosition / ChunkSize), voxels[i]))
                        {
                            Vector2 uv00 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Back, 1, 1);
                            Vector2 uv01 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Back, 1, 0);
                            Vector2 uv10 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Back, 0, 1);
                            Vector2 uv11 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Back, 0, 0);
                            
                            if (VoxelData.IsLiquid(uvoxelID))
                            {
                                uint aboveVoxelId =
                                    Engine.GetVoxelAtPosition(voxelPosition + new Vector3Int(0, 1, 0)).voxelId ?? 0;
                                bool isLiquidAbove = VoxelData.IsLiquid(aboveVoxelId);

                                if (!isLiquidAbove)
                                {
                                    vertices.AddRange(new Vector3[]
                                    {
                                        new(x - 0.5f, y - 0.5f, z + 0.5f),
                                        new(x - 0.5f, y + 0.5f, z + 0.5f),
                                        new(x + 0.5f, y - 0.5f, z + 0.5f),
                                        new(x + 0.5f, y + 0.5f, z + 0.5f),
                                    });
                                }
                                else
                                {
                                    vertices.AddRange(new Vector3[]
                                    {
                                        new(x - 0.5f, y - 0.5f, z + 0.5f),
                                        new(x - 0.5f, y + liquidHeight, z + 0.5f),
                                        new(x + 0.5f, y - 0.5f, z + 0.5f),
                                        new(x + 0.5f, y + liquidHeight, z + 0.5f),
                                    });
                                }
                            }
                            else
                            {
                                vertices.AddRange(new Vector3[]
                                {
                                    new(x - 0.5f, y - 0.5f, z + 0.5f),
                                    new(x - 0.5f, y + 0.5f, z + 0.5f),
                                    new(x + 0.5f, y - 0.5f, z + 0.5f),
                                    new(x + 0.5f, y + 0.5f, z + 0.5f),
                                });
                            }
                            
                            for (int j = 0; j < 4; j++)
                                normals.Add(new Vector3(0, 0, 1));
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

                        // Top face
                        if (IsTransparent(voxelPosition.X, voxelPosition.Y + 1, voxelPosition.Z, voxels, chunks,
                                (Vector3Int)(chunkPosition / ChunkSize), voxels[i]))
                        {
                            Vector2 uv00 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Top, 1, 1);
                            Vector2 uv01 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Top, 1, 0);
                            Vector2 uv10 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Top, 0, 1);
                            Vector2 uv11 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Top, 0, 0);

                            if (VoxelData.IsLiquid(uvoxelID))
                            {
                                uint aboveVoxelId =
                                    Engine.GetVoxelAtPosition(voxelPosition + new Vector3Int(0, 1, 0)).voxelId ?? 0;
                                bool isLiquidAbove = VoxelData.IsLiquid(aboveVoxelId);

                                if (!isLiquidAbove)
                                {
                                    vertices.AddRange(new Vector3[]
                                    {
                                        new(x - 0.5f, y + 0.5f, z - 0.5f),
                                        new(x + 0.5f, y + 0.5f, z - 0.5f),
                                        new(x - 0.5f, y + 0.5f, z + 0.5f),
                                        new(x + 0.5f, y + 0.5f, z + 0.5f),
                                    });
                                }
                                else
                                {
                                    vertices.AddRange(new Vector3[]
                                    {
                                        new(x - 0.5f, y + liquidHeight, z - 0.5f),
                                        new(x + 0.5f, y + liquidHeight, z - 0.5f),
                                        new(x - 0.5f, y + liquidHeight, z + 0.5f),
                                        new(x + 0.5f, y + liquidHeight, z + 0.5f),
                                    });
                                }
                            }
                            else
                            {
                                vertices.AddRange(new Vector3[]
                                {
                                    new(x - 0.5f, y + 0.5f, z - 0.5f),
                                    new(x + 0.5f, y + 0.5f, z - 0.5f),
                                    new(x - 0.5f, y + 0.5f, z + 0.5f),
                                    new(x + 0.5f, y + 0.5f, z + 0.5f),
                                });
                            }
                            
                            for (int j = 0; j < 4; j++)
                                normals.Add(new Vector3(0, 1, 0));
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

                        // Bottom face
                        if (IsTransparent(voxelPosition.X, voxelPosition.Y - 1, voxelPosition.Z, voxels, chunks,
                                (Vector3Int)(chunkPosition / ChunkSize), voxels[i]))
                        {
                            Vector2 uv00 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Bottom, 1, 1);
                            Vector2 uv01 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Bottom, 1, 0);
                            Vector2 uv10 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Bottom, 0, 1);
                            Vector2 uv11 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Bottom, 0, 0);

                            if (VoxelData.IsLiquid(uvoxelID))
                            {
                                uint aboveVoxelId =
                                    Engine.GetVoxelAtPosition(voxelPosition + new Vector3Int(0, 1, 0)).voxelId ?? 0;
                                bool isLiquidAbove = VoxelData.IsLiquid(aboveVoxelId);

                                if (!isLiquidAbove)
                                {
                                    vertices.AddRange(new Vector3[]
                                    {
                                        new(x - 0.5f, y - 0.5f, z - 0.5f),
                                        new(x + 0.5f, y - 0.5f, z - 0.5f),
                                        new(x - 0.5f, y - 0.5f, z + 0.5f),
                                        new(x + 0.5f, y - 0.5f, z + 0.5f),
                                    });
                                }
                                else
                                {
                                    vertices.AddRange(new Vector3[]
                                    {
                                        new(x - 0.5f, y - liquidHeight, z - 0.5f),
                                        new(x + 0.5f, y - liquidHeight, z - 0.5f),
                                        new(x - 0.5f, y - liquidHeight, z + 0.5f),
                                        new(x + 0.5f, y - liquidHeight, z + 0.5f),
                                    });
                                }
                            }
                            else
                            {
                                vertices.AddRange(new Vector3[]
                                {
                                    new(x - 0.5f, y - 0.5f, z - 0.5f),
                                    new(x + 0.5f, y - 0.5f, z - 0.5f),
                                    new(x - 0.5f, y - 0.5f, z + 0.5f),
                                    new(x + 0.5f, y - 0.5f, z + 0.5f),
                                });
                            }
                            
                            for (int j = 0; j < 4; j++)
                                normals.Add(new Vector3(0, -1, 0));
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

                        // Right face
                        if (IsTransparent(voxelPosition.X + 1, voxelPosition.Y, voxelPosition.Z, voxels, chunks,
                                (Vector3Int)(chunkPosition / ChunkSize), voxels[i]))
                        {
                            Vector2 uv00 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Right, 1, 1);
                            Vector2 uv01 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Right, 1, 0);
                            Vector2 uv10 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Right, 0, 1);
                            Vector2 uv11 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Right, 0, 0);

                            if (VoxelData.IsLiquid(uvoxelID))
                            {
                                uint aboveVoxelId =
                                    Engine.GetVoxelAtPosition(voxelPosition + new Vector3Int(0, 1, 0)).voxelId ?? 0;
                                bool isLiquidAbove = VoxelData.IsLiquid(aboveVoxelId);

                                if (!isLiquidAbove)
                                {
                                    vertices.AddRange(new Vector3[]
                                    {
                                        new(x + 0.5f, y - 0.5f, z - 0.5f),
                                        new(x + 0.5f, y + 0.5f, z - 0.5f),
                                        new(x + 0.5f, y - 0.5f, z + 0.5f),
                                        new(x + 0.5f, y + 0.5f, z + 0.5f),
                                    });
                                }
                                else
                                {
                                    vertices.AddRange(new Vector3[]
                                    {
                                        new(x + 0.5f, y - 0.5f, z - 0.5f),
                                        new(x + 0.5f, y + liquidHeight, z - 0.5f),
                                        new(x + 0.5f, y - 0.5f, z + 0.5f),
                                        new(x + 0.5f, y + liquidHeight, z + 0.5f),
                                    });
                                }
                            }
                            else
                            {
                                vertices.AddRange(new Vector3[]
                                {
                                    new(x + 0.5f, y - 0.5f, z - 0.5f),
                                    new(x + 0.5f, y + 0.5f, z - 0.5f),
                                    new(x + 0.5f, y - 0.5f, z + 0.5f),
                                    new(x + 0.5f, y + 0.5f, z + 0.5f),
                                });
                            }
                            
                            for (int j = 0; j < 4; j++)
                                normals.Add(new Vector3(1, 0, 0));
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

                        // Left face
                        if (IsTransparent(voxelPosition.X - 1, voxelPosition.Y, voxelPosition.Z, voxels, chunks,
                                (Vector3Int)(chunkPosition / ChunkSize), voxels[i]))
                        {
                            Vector2 uv00 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Left, 1, 1);
                            Vector2 uv01 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Left, 1, 0);
                            Vector2 uv10 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Left, 0, 1);
                            Vector2 uv11 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Left, 0, 0);

                            if (VoxelData.IsLiquid(uvoxelID))
                            {
                                uint aboveVoxelId =
                                    Engine.GetVoxelAtPosition(voxelPosition + new Vector3Int(0, 1, 0)).voxelId ?? 0;
                                bool isLiquidAbove = VoxelData.IsLiquid(aboveVoxelId);

                                if (!isLiquidAbove)
                                {
                                    vertices.AddRange(new Vector3[]
                                    {
                                        new(x - 0.5f, y - 0.5f, z - 0.5f),
                                        new(x - 0.5f, y + 0.5f, z - 0.5f),
                                        new(x - 0.5f, y - 0.5f, z + 0.5f),
                                        new(x - 0.5f, y + 0.5f, z + 0.5f),
                                    });
                                }
                                else
                                {
                                    vertices.AddRange(new Vector3[]
                                    {
                                        new(x - 0.5f, y - 0.5f, z - 0.5f),
                                        new(x - 0.5f, y + liquidHeight, z - 0.5f),
                                        new(x - 0.5f, y - 0.5f, z + 0.5f),
                                        new(x - 0.5f, y + liquidHeight, z + 0.5f),
                                    });
                                }
                            }
                            else
                            {
                                vertices.AddRange(new Vector3[]
                                {
                                    new(x - 0.5f, y - 0.5f, z - 0.5f),
                                    new(x - 0.5f, y + 0.5f, z - 0.5f),
                                    new(x - 0.5f, y - 0.5f, z + 0.5f),
                                    new(x - 0.5f, y + 0.5f, z + 0.5f),
                                });
                            }
                            
                            for (int j = 0; j < 4; j++)
                                normals.Add(new Vector3(-1, 0, 1));
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
                    }
                    else
                    {
                        // Front face
                        if (IsTransparent(voxelPosition.X, voxelPosition.Y, voxelPosition.Z - 1, voxels, chunks,
                                (Vector3Int)(chunkPosition / ChunkSize), voxels[i]))
                        {
                            Vector2 uv00 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Front, 1, 1);
                            Vector2 uv01 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Front, 1, 0);
                            Vector2 uv10 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Front, 0, 1);
                            Vector2 uv11 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Front, 0, 0);

                            if (VoxelData.IsLiquid(uvoxelID))
                            {
                                uint aboveVoxelId =
                                    Engine.GetVoxelAtPosition(voxelPosition + new Vector3Int(0, 1, 0)).voxelId ?? 0;
                                bool isLiquidAbove = VoxelData.IsLiquid(aboveVoxelId);

                                if (!isLiquidAbove)
                                {
                                    transparentVertices.AddRange(new Vector3[]
                                    {
                                        new(x - 0.5f, y - 0.5f, z - 0.5f),
                                        new(x - 0.5f, y + 0.5f, z - 0.5f),
                                        new(x + 0.5f, y - 0.5f, z - 0.5f),
                                        new(x + 0.5f, y + 0.5f, z - 0.5f),
                                    });
                                }
                                else
                                {
                                    transparentVertices.AddRange(new Vector3[]
                                    {
                                        new(x - 0.5f, y - 0.5f, z - 0.5f),
                                        new(x - 0.5f, y + liquidHeight, z - 0.5f),
                                        new(x + 0.5f, y - 0.5f, z - 0.5f),
                                        new(x + 0.5f, y + liquidHeight, z - 0.5f),
                                    });
                                }
                            }
                            else
                            {
                                transparentVertices.AddRange(new Vector3[]
                                {
                                    new(x - 0.5f, y - 0.5f, z - 0.5f),
                                    new(x - 0.5f, y + 0.5f, z - 0.5f),
                                    new(x + 0.5f, y - 0.5f, z - 0.5f),
                                    new(x + 0.5f, y + 0.5f, z - 0.5f),
                                });
                            }
                            
                            for (int j = 0; j < 4; j++)
                                transparentNormals.Add(new Vector3(0, 0, -1));
                            transparentUvs.AddRange(new[]
                            {
                                uv00, uv01, uv10, uv11
                            });
                            transparentTriangles.AddRange(new[]
                            {
                                0 + transparentTriangleIndex, 2 + transparentTriangleIndex, 1 + transparentTriangleIndex,
                                2 + transparentTriangleIndex, 3 + transparentTriangleIndex, 1 + transparentTriangleIndex,
                            });
                            transparentTriangleIndex += 4;
                        }

                        // Back face
                        if (IsTransparent(voxelPosition.X, voxelPosition.Y, voxelPosition.Z + 1, voxels, chunks,
                                (Vector3Int)(chunkPosition / ChunkSize), voxels[i]))
                        {
                            Vector2 uv00 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Back, 1, 1);
                            Vector2 uv01 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Back, 1, 0);
                            Vector2 uv10 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Back, 0, 1);
                            Vector2 uv11 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Back, 0, 0);
                            
                            if (VoxelData.IsLiquid(uvoxelID))
                            {
                                uint aboveVoxelId =
                                    Engine.GetVoxelAtPosition(voxelPosition + new Vector3Int(0, 1, 0)).voxelId ?? 0;
                                bool isLiquidAbove = VoxelData.IsLiquid(aboveVoxelId);

                                if (!isLiquidAbove)
                                {
                                    transparentVertices.AddRange(new Vector3[]
                                    {
                                        new(x - 0.5f, y - 0.5f, z + 0.5f),
                                        new(x - 0.5f, y + 0.5f, z + 0.5f),
                                        new(x + 0.5f, y - 0.5f, z + 0.5f),
                                        new(x + 0.5f, y + 0.5f, z + 0.5f),
                                    });
                                }
                                else
                                {
                                    transparentVertices.AddRange(new Vector3[]
                                    {
                                        new(x - 0.5f, y - 0.5f, z + 0.5f),
                                        new(x - 0.5f, y + liquidHeight, z + 0.5f),
                                        new(x + 0.5f, y - 0.5f, z + 0.5f),
                                        new(x + 0.5f, y + liquidHeight, z + 0.5f),
                                    });
                                }
                            }
                            else
                            {
                                transparentVertices.AddRange(new Vector3[]
                                {
                                    new(x - 0.5f, y - 0.5f, z + 0.5f),
                                    new(x - 0.5f, y + 0.5f, z + 0.5f),
                                    new(x + 0.5f, y - 0.5f, z + 0.5f),
                                    new(x + 0.5f, y + 0.5f, z + 0.5f),
                                });
                            }
                            
                            for (int j = 0; j < 4; j++)
                                transparentNormals.Add(new Vector3(0, 0, 1));
                            transparentUvs.AddRange(new[]
                            {
                                uv00, uv01, uv10, uv11
                            });
                            transparentTriangles.AddRange(new[]
                            {
                                0 + transparentTriangleIndex, 1 + transparentTriangleIndex, 2 + transparentTriangleIndex,
                                2 + transparentTriangleIndex, 1 + transparentTriangleIndex, 3 + transparentTriangleIndex,
                            });
                            transparentTriangleIndex += 4;
                        }

                        // Top face
                        if (IsTransparent(voxelPosition.X, voxelPosition.Y + 1, voxelPosition.Z, voxels, chunks,
                                (Vector3Int)(chunkPosition / ChunkSize), voxels[i]))
                        {
                            Vector2 uv00 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Top, 1, 1);
                            Vector2 uv01 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Top, 1, 0);
                            Vector2 uv10 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Top, 0, 1);
                            Vector2 uv11 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Top, 0, 0);

                            if (VoxelData.IsLiquid(uvoxelID))
                            {
                                uint aboveVoxelId =
                                    Engine.GetVoxelAtPosition(voxelPosition + new Vector3Int(0, 1, 0)).voxelId ?? 0;
                                bool isLiquidAbove = VoxelData.IsLiquid(aboveVoxelId);

                                if (!isLiquidAbove)
                                {
                                    transparentVertices.AddRange(new Vector3[]
                                    {
                                        new(x - 0.5f, y + 0.5f, z - 0.5f),
                                        new(x + 0.5f, y + 0.5f, z - 0.5f),
                                        new(x - 0.5f, y + 0.5f, z + 0.5f),
                                        new(x + 0.5f, y + 0.5f, z + 0.5f),
                                    });
                                }
                                else
                                {
                                    transparentVertices.AddRange(new Vector3[]
                                    {
                                        new(x - 0.5f, y + liquidHeight, z - 0.5f),
                                        new(x + 0.5f, y + liquidHeight, z - 0.5f),
                                        new(x - 0.5f, y + liquidHeight, z + 0.5f),
                                        new(x + 0.5f, y + liquidHeight, z + 0.5f),
                                    });
                                }
                            }
                            else
                            {
                                transparentVertices.AddRange(new Vector3[]
                                {
                                    new(x - 0.5f, y + 0.5f, z - 0.5f),
                                    new(x + 0.5f, y + 0.5f, z - 0.5f),
                                    new(x - 0.5f, y + 0.5f, z + 0.5f),
                                    new(x + 0.5f, y + 0.5f, z + 0.5f),
                                });
                            }
                            
                            for (int j = 0; j < 4; j++)
                                transparentNormals.Add(new Vector3(0, 1, 0));
                            transparentUvs.AddRange(new[]
                            {
                                uv00, uv01, uv10, uv11
                            });
                            transparentTriangles.AddRange(new[]
                            {
                                0 + transparentTriangleIndex, 1 + transparentTriangleIndex, 2 + transparentTriangleIndex,
                                2 + transparentTriangleIndex, 1 + transparentTriangleIndex, 3 + transparentTriangleIndex,
                            });
                            transparentTriangleIndex += 4;
                        }

                        // Bottom face
                        if (IsTransparent(voxelPosition.X, voxelPosition.Y - 1, voxelPosition.Z, voxels, chunks,
                                (Vector3Int)(chunkPosition / ChunkSize), voxels[i]))
                        {
                            Vector2 uv00 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Bottom, 1, 1);
                            Vector2 uv01 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Bottom, 1, 0);
                            Vector2 uv10 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Bottom, 0, 1);
                            Vector2 uv11 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Bottom, 0, 0);

                            if (VoxelData.IsLiquid(uvoxelID))
                            {
                                uint aboveVoxelId =
                                    Engine.GetVoxelAtPosition(voxelPosition + new Vector3Int(0, 1, 0)).voxelId ?? 0;
                                bool isLiquidAbove = VoxelData.IsLiquid(aboveVoxelId);

                                if (!isLiquidAbove)
                                {
                                    transparentVertices.AddRange(new Vector3[]
                                    {
                                        new(x - 0.5f, y - 0.5f, z - 0.5f),
                                        new(x + 0.5f, y - 0.5f, z - 0.5f),
                                        new(x - 0.5f, y - 0.5f, z + 0.5f),
                                        new(x + 0.5f, y - 0.5f, z + 0.5f),
                                    });
                                }
                                else
                                {
                                    transparentVertices.AddRange(new Vector3[]
                                    {
                                        new(x - 0.5f, y - liquidHeight, z - 0.5f),
                                        new(x + 0.5f, y - liquidHeight, z - 0.5f),
                                        new(x - 0.5f, y - liquidHeight, z + 0.5f),
                                        new(x + 0.5f, y - liquidHeight, z + 0.5f),
                                    });
                                }
                            }
                            else
                            {
                                transparentVertices.AddRange(new Vector3[]
                                {
                                    new(x - 0.5f, y - 0.5f, z - 0.5f),
                                    new(x + 0.5f, y - 0.5f, z - 0.5f),
                                    new(x - 0.5f, y - 0.5f, z + 0.5f),
                                    new(x + 0.5f, y - 0.5f, z + 0.5f),
                                });
                            }
                            
                            for (int j = 0; j < 4; j++)
                                transparentNormals.Add(new Vector3(0, -1, 0));
                            transparentUvs.AddRange(new[]
                            {
                                uv00, uv01, uv10, uv11
                            });
                            transparentTriangles.AddRange(new[]
                            {
                                0 + transparentTriangleIndex, 2 + transparentTriangleIndex, 1 + transparentTriangleIndex,
                                2 + transparentTriangleIndex, 3 + transparentTriangleIndex, 1 + transparentTriangleIndex,
                            });
                            transparentTriangleIndex += 4;
                        }

                        // Right face
                        if (IsTransparent(voxelPosition.X + 1, voxelPosition.Y, voxelPosition.Z, voxels, chunks,
                                (Vector3Int)(chunkPosition / ChunkSize), voxels[i]))
                        {
                            Vector2 uv00 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Right, 1, 1);
                            Vector2 uv01 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Right, 1, 0);
                            Vector2 uv10 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Right, 0, 1);
                            Vector2 uv11 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Right, 0, 0);

                            if (VoxelData.IsLiquid(uvoxelID))
                            {
                                uint aboveVoxelId =
                                    Engine.GetVoxelAtPosition(voxelPosition + new Vector3Int(0, 1, 0)).voxelId ?? 0;
                                bool isLiquidAbove = VoxelData.IsLiquid(aboveVoxelId);

                                if (!isLiquidAbove)
                                {
                                    transparentVertices.AddRange(new Vector3[]
                                    {
                                        new(x + 0.5f, y - 0.5f, z - 0.5f),
                                        new(x + 0.5f, y + 0.5f, z - 0.5f),
                                        new(x + 0.5f, y - 0.5f, z + 0.5f),
                                        new(x + 0.5f, y + 0.5f, z + 0.5f),
                                    });
                                }
                                else
                                {
                                    transparentVertices.AddRange(new Vector3[]
                                    {
                                        new(x + 0.5f, y - 0.5f, z - 0.5f),
                                        new(x + 0.5f, y + liquidHeight, z - 0.5f),
                                        new(x + 0.5f, y - 0.5f, z + 0.5f),
                                        new(x + 0.5f, y + liquidHeight, z + 0.5f),
                                    });
                                }
                            }
                            else
                            {
                                transparentVertices.AddRange(new Vector3[]
                                {
                                    new(x + 0.5f, y - 0.5f, z - 0.5f),
                                    new(x + 0.5f, y + 0.5f, z - 0.5f),
                                    new(x + 0.5f, y - 0.5f, z + 0.5f),
                                    new(x + 0.5f, y + 0.5f, z + 0.5f),
                                });
                            }
                            
                            for (int j = 0; j < 4; j++)
                                transparentNormals.Add(new Vector3(1, 0, 0));
                            transparentUvs.AddRange(new[]
                            {
                                uv00, uv01, uv10, uv11
                            });
                            transparentTriangles.AddRange(new[]
                            {
                                0 + transparentTriangleIndex, 2 + transparentTriangleIndex, 1 + transparentTriangleIndex,
                                2 + transparentTriangleIndex, 3 + transparentTriangleIndex, 1 + transparentTriangleIndex,
                            });
                            transparentTriangleIndex += 4;
                        }

                        // Left face
                        if (IsTransparent(voxelPosition.X - 1, voxelPosition.Y, voxelPosition.Z, voxels, chunks,
                                (Vector3Int)(chunkPosition / ChunkSize), voxels[i]))
                        {
                            Vector2 uv00 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Left, 1, 1);
                            Vector2 uv01 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Left, 1, 0);
                            Vector2 uv10 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Left, 0, 1);
                            Vector2 uv11 = TextureAtlas.GetUVForVoxelFace(voxelID - 1, VoxelFace.Left, 0, 0);

                            if (VoxelData.IsLiquid(uvoxelID))
                            {
                                uint aboveVoxelId =
                                    Engine.GetVoxelAtPosition(voxelPosition + new Vector3Int(0, 1, 0)).voxelId ?? 0;
                                bool isLiquidAbove = VoxelData.IsLiquid(aboveVoxelId);

                                if (!isLiquidAbove)
                                {
                                    transparentVertices.AddRange(new Vector3[]
                                    {
                                        new(x - 0.5f, y - 0.5f, z - 0.5f),
                                        new(x - 0.5f, y + 0.5f, z - 0.5f),
                                        new(x - 0.5f, y - 0.5f, z + 0.5f),
                                        new(x - 0.5f, y + 0.5f, z + 0.5f),
                                    });
                                }
                                else
                                {
                                    transparentVertices.AddRange(new Vector3[]
                                    {
                                        new(x - 0.5f, y - 0.5f, z - 0.5f),
                                        new(x - 0.5f, y + liquidHeight, z - 0.5f),
                                        new(x - 0.5f, y - 0.5f, z + 0.5f),
                                        new(x - 0.5f, y + liquidHeight, z + 0.5f),
                                    });
                                }
                            }
                            else
                            {
                                transparentVertices.AddRange(new Vector3[]
                                {
                                    new(x - 0.5f, y - 0.5f, z - 0.5f),
                                    new(x - 0.5f, y + 0.5f, z - 0.5f),
                                    new(x - 0.5f, y - 0.5f, z + 0.5f),
                                    new(x - 0.5f, y + 0.5f, z + 0.5f),
                                });
                            }
                            
                            for (int j = 0; j < 4; j++)
                                transparentNormals.Add(new Vector3(-1, 0, 1));
                            transparentUvs.AddRange(new[]
                            {
                                uv00, uv01, uv10, uv11
                            });
                            transparentTriangles.AddRange(new[]
                            {
                                0 + transparentTriangleIndex, 1 + transparentTriangleIndex, 2 + transparentTriangleIndex,
                                2 + transparentTriangleIndex, 1 + transparentTriangleIndex, 3 + transparentTriangleIndex,
                            });
                            transparentTriangleIndex += 4;
                        }
                    }
                }
            }

            _vertexCount = vertices.Count;
            _triangleCount = triangles.Count;
            _transparentTriangleCount = transparentTriangles.Count;

            var data = new float[vertices.Count * Stride];
                
            for (int i = 0; i < vertices.Count; i++)
            {
                data[i * Stride + 0] = vertices[i].X;
                data[i * Stride + 1] = vertices[i].Y;
                data[i * Stride + 2] = vertices[i].Z;
                
                data[i * Stride + 3] = normals[i].X;
                data[i * Stride + 4] = normals[i].Y;
                data[i * Stride + 5] = normals[i].Z;
                
                data[i * Stride + 6] = uvs[i].X;
                data[i * Stride + 7] = uvs[i].Y;
            }
            
            var transparentData = new float[transparentVertices.Count * Stride];
                
            for (int i = 0; i < transparentVertices.Count; i++)
            {
                transparentData[i * Stride + 0] = transparentVertices[i].X;
                transparentData[i * Stride + 1] = transparentVertices[i].Y;
                transparentData[i * Stride + 2] = transparentVertices[i].Z;
                
                transparentData[i * Stride + 3] = transparentNormals[i].X;
                transparentData[i * Stride + 4] = transparentNormals[i].Y;
                transparentData[i * Stride + 5] = transparentNormals[i].Z;
                
                transparentData[i * Stride + 6] = transparentUvs[i].X;
                transparentData[i * Stride + 7] = transparentUvs[i].Y;
            }

            if (_triangleCount <= 0)
                IsEmpty = true;

            var transparentTrianglesArr = transparentTriangles.ToArray();
            var transparentVerticesArr = transparentVertices.ToArray();

            return (true, data, triangles.ToArray(), containsTransparentVoxels, transparentVerticesArr, transparentData, transparentTrianglesArr);
        }

        return (false, null, null, false, null, null, null);
    }
    
    internal void RebuildChunk(Dictionary<Vector3Int, Chunk> chunks, Vector3 cameraPos, bool recursive = false)
    {
        // Console.WriteLine($"Chunk at position {chunkPosition} has been rebuilt!");
        
        BuildChunk(chunks, cameraPos);

        Vector3Int[] neighbourChunkOffsets =
        [
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0)
        ];

        if (recursive)
        {
            foreach (var offset in neighbourChunkOffsets)
            {
                if (chunks.TryGetValue(ChunkSpacePosition + offset, out var neighbour))
                {
                    neighbour.BuildChunk(chunks, cameraPos);
                }
            }
        }
        
        IsDirty = true;
    }

    public static bool IsTransparent(int x, int y, int z, uint[] voxels, Dictionary<Vector3Int, Chunk> chunks, Vector3Int currentChunkPosition, uint selfVoxelId)
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
                return VoxelData.IsTransparent(neighborChunk.voxels[index], selfVoxelId);
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
            return VoxelData.IsTransparent(voxels[index], selfVoxelId);
        }
    }

    public static bool IsVoxelVisible(int x, int y, int z, uint[] voxels, Dictionary<Vector3Int, Chunk> chunks,
        Vector3Int currentChunkPosition, uint selfVoxelId)
    {
        bool up    = IsTransparent(  x,   y + 1, z, voxels, chunks, currentChunkPosition, selfVoxelId);
        bool down  = IsTransparent(  x,   y - 1, z, voxels, chunks, currentChunkPosition, selfVoxelId);
        bool front = IsTransparent(  x,     y,   z + 1, voxels, chunks, currentChunkPosition, selfVoxelId);
        bool back  = IsTransparent(  x,     y,   z - 1, voxels, chunks, currentChunkPosition, selfVoxelId);
        bool right = IsTransparent(x + 1, y,     z, voxels, chunks, currentChunkPosition, selfVoxelId);
        bool left  = IsTransparent(x - 1, y,     z, voxels, chunks, currentChunkPosition, selfVoxelId);

        return up || down || front || back || right || left;
    }
    
    public static int FlattenIndex3D(int x, int y, int z, int width, int height)
    {
        return x + (y * width) + (z * width * height);
    }

    internal (int vertexCount, int triangleCount) Render(Player player, ShadowMapper shadowMapper)
    {
        if (IsEmpty || !IsRenderReady)
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

        GL.BindVertexArray(0);
        
        if (ContainsTransparentVoxels)
        {
            Engine.RegisterTransparentChunkDrawCall(Vector3.SqrDistance(chunkCentre, player.Position), () =>
            {
                TextureAtlas.AlbedoTexture.Use(TextureUnit.Texture0);
                TextureAtlas.SpecularTexture.Use(TextureUnit.Texture1);
                
                SortTriangles(player.Position);
                
                _transparentShader.Use();
                _transparentShader.SetUniform("m_proj", ref player.ProjectionMatrix, autoUse: false);
                _transparentShader.SetUniform("m_view", ref player.ViewMatrix, autoUse: false);
                _transparentShader.SetUniform("m_model", ref m_model, autoUse: false);
                
                GL.BindVertexArray(_transparentVao);
                TextureAtlas.AlbedoTexture.Use(TextureUnit.Texture0);
                GL.DrawElements(PrimitiveType.Triangles, _transparentTriangleCount, DrawElementsType.UnsignedInt, 0);
                GL.BindVertexArray(0);
            });
        }
        
        Engine.CheckGLError("Chunk render (normal pass)");

        return (_vertexCount, _triangleCount / 3);
    }
    public (int vertexCount, int triangleCount) Render(Matrix4 m_proj, Matrix4 m_view, Shader shaderOverride,
        bool overrideShader = false)
    {
        if (IsEmpty || !IsRenderReady)
            return (0, 0);

        Engine.BatchCount++;
        
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
        
        Engine.CheckGLError("Chunk render (shadow pass)");
        
        GL.BindVertexArray(0);

        return (_vertexCount, _triangleCount / 3);
    }

    internal void OnSaved() => IsDirty = false;

    internal AABB[] GenerateCollisions(Dictionary<Vector3Int, Chunk> chunks)
    {
        List<AABB> collisions = new();
        int index = 0;
        for (int x = 0; x < ChunkSize; x++)
        {
            for (int y = 0; y < ChunkSize; y++)
            {
                for (int z = 0; z < ChunkSize; z++)
                {
                    int voxelIndex = FlattenIndex3D(x, y, z, ChunkSize, ChunkSize);
                    uint voxel = voxels[voxelIndex];
                    if (voxel != 0 && IsVoxelVisible(x, y, z, voxels, chunks, (Vector3Int)(chunkPosition / ChunkSize), voxel))
                    {
                        Vector3 min = new Vector3(chunkPosition.X + x, chunkPosition.Y + y, chunkPosition.Z + z);
                        Vector3 max = min + Vector3.One / 2f;
                        collisions.Add(new AABB(min, max));
                        index++;
                    }
                }
            }
        }

        return collisions.ToArray();
    }
    
    internal void SortTriangles(Vector3 cameraPos)
    {
        SortedList<(float distance, int index), Triangle> sortedTriangles = new(new DescComparer<(float, int)>());
        ConcurrentArray<((float distance, int index), Triangle tri)> arr = new(_transparentTriangles.Length / 3);
        
        Parallel.For(0, _transparentTriangles.Length / 3, i =>
        {
            int triIndex = i * 3;
            
            // Extract actual vertex indices from the _triangles array
            int index0 = _transparentTriangles[triIndex];
            int index1 = _transparentTriangles[triIndex + 1];
            int index2 = _transparentTriangles[triIndex + 2];

            Triangle triangle = new Triangle(index0, index1, index2);

            var points = triangle.GetPoints(_transparentVertices);
            Vector3 p1 = points.p1;
            Vector3 p2 = points.p2;
            Vector3 p3 = points.p3;
            
            Vector3 centerPoint = (p1 + p2 + p3) / 3f;

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
            _transparentTriangles[idx] = triangle.a;
            _transparentTriangles[idx + 1] = triangle.b;
            _transparentTriangles[idx + 2] = triangle.c;
            idx += 3;
        }

        GL.BindVertexArray(_transparentVao);
        
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _transparentEbo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _transparentTriangles.Length * sizeof(int), _transparentTriangles, BufferUsageHint.StaticDraw);
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