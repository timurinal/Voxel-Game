using OpenTK.Mathematics;
using VoxelGame.Maths;
using VoxelGame.TerrainGeneration;
using Vector2 = VoxelGame.Maths.Vector2;
using Vector3 = VoxelGame.Maths.Vector3;

namespace VoxelGame.Rendering;

internal class Chunk
{
    public const int ChunkSize = 32;
    public const int HChunkSize = ChunkSize / 2;
    public const int ChunkArea = ChunkSize * ChunkSize;
    public const int ChunkVolume = ChunkArea * ChunkSize;

    public Vector3Int Position;
    public Vector3 Centre =>
        new(Position.X + HChunkSize, 
            Position.Y + HChunkSize,
            Position.Z + HChunkSize);

    public AABB Bounds;

    public int SolidVoxelCount;
    public bool IsEmpty => SolidVoxelCount <= 0;
    
    public bool IsRenderReady { get; private set; }
    
    public uint[,,] Voxels;

    private Mesh _opaqueMesh;

    public Chunk(Vector3Int position, Material material)
    {
        Position = position * ChunkSize;
        
        Bounds = AABB.CreateFromExtents(Centre, Vector3.One * (HChunkSize + 1));
        
        Voxels = new uint[ChunkSize, ChunkSize, ChunkSize];
        
        SolidVoxelCount = 0;
        
        for (int x = 0; x < ChunkSize; x++)
        {
            for (int y = 0; y < ChunkSize; y++)
            {
                for (int z = 0; z < ChunkSize; z++)
                {
                    int globalX = x + Position.X;
                    int globalY = y + Position.Y;
                    int globalZ = z + Position.Z;
            
                    float val = TerrainGenerator.Sample(globalX, globalY, globalZ, scale: 0.1f);
                    
                    uint vox = val >= 0.5f ? 1u : 0u;
                    Voxels[x, y, z] = vox;

                    SolidVoxelCount += vox != 0u ? 1 : 0;
                }
            }
        }

        _opaqueMesh = new Mesh(material);
        _opaqueMesh.Transform.Position = Position;
    }

    internal async void BuildChunk()
    {
        var data = await Task.Run(GenerateChunkData);
        
        // This is run on the main thread,
        // so it is safe to make GL calls (not render calls though)
        Engine.RunOnMainThread(() =>
        {
            if (data.opaqueData.hasData)
            {
                _opaqueMesh.Vertices = data.opaqueData.vertices;
                _opaqueMesh.Normals = data.opaqueData.normals;
                _opaqueMesh.Tangents = data.opaqueData.tangents;
                _opaqueMesh.Uvs = data.opaqueData.uvs;
            
                _opaqueMesh.Triangles = data.opaqueData.triangles;
            }
        });
    }

    private ChunkData GenerateChunkData()
    {
        // This function is likely going to be running on a background thread,
        // so don't make any GL calls or any non thread-safe function calls.
        // Instead, wait and call those inside the marked function in BuildChunk,
        // that is always executed on the main thread.
        if (!IsEmpty)
        {
            List<Vector3> vertices = new();
            List<Vector3> normals = new();
            List<Vector3> tangents = new();
            List<Vector2> uvs = new();
            List<int> triangles = new();
            
            for (int x = 0, triangleIndex = 0; x < ChunkSize; x++)
            {
                for (int y = 0; y < ChunkSize; y++)
                {
                    for (int z = 0; z < ChunkSize; z++)
                    {
                        uint voxel = Voxels[x, y, z];
                        
                        // 0 means air, so skip adding any faces for this voxel
                        if (voxel == 0)
                            continue;
                        
                        #region Chunk Builder
                        
                        // TODO: cull faces between voxels
                        
                        // Front face
                        if (IsTransparent(x, y, z - 1, Voxels))
                        {
                            vertices.AddRange(new Vector3[] {
                                new Vector3(x - 0.5f, y - 0.5f, z - 0.5f),
                                new Vector3(x - 0.5f, y + 0.5f, z - 0.5f),
                                new Vector3(x + 0.5f, y - 0.5f, z - 0.5f),
                                new Vector3(x + 0.5f, y + 0.5f, z - 0.5f),
                            });

                            normals.AddRange(new Vector3[] {
                                new Vector3(0, 0, -1),
                                new Vector3(0, 0, -1),
                                new Vector3(0, 0, -1),
                                new Vector3(0, 0, -1),
                            });
                            tangents.AddRange(new Vector3[] {
                                new Vector3(-1, 0, 0),
                                new Vector3(-1, 0, 0),
                                new Vector3(-1, 0, 0),
                                new Vector3(-1, 0, 0),
                            });
                            
                            uvs.AddRange(new Vector2[] {
                                new Vector2(1, 0),
                                new Vector2(1, 1),
                                new Vector2(0, 0),
                                new Vector2(0, 1),
                            });
                            
                            triangles.AddRange(new int[] {
                                0 + triangleIndex, 2 + triangleIndex, 1 + triangleIndex,
                                2 + triangleIndex, 3 + triangleIndex, 1 + triangleIndex
                            });

                            triangleIndex += 4;
                        }
                        // Back face
                        if (IsTransparent(x, y, z + 1, Voxels))
                        {
                            vertices.AddRange(new Vector3[] {
                                new Vector3(x - 0.5f, y - 0.5f, z + 0.5f),
                                new Vector3(x - 0.5f, y + 0.5f, z + 0.5f),
                                new Vector3(x + 0.5f, y - 0.5f, z + 0.5f),
                                new Vector3(x + 0.5f, y + 0.5f, z + 0.5f),
                            });

                            normals.AddRange(new Vector3[] {
                                new Vector3(0, 0, 1),
                                new Vector3(0, 0, 1),
                                new Vector3(0, 0, 1),
                                new Vector3(0, 0, 1),
                            });
                            tangents.AddRange(new Vector3[] {
                                new Vector3(1, 0, 0),
                                new Vector3(1, 0, 0),
                                new Vector3(1, 0, 0),
                                new Vector3(1, 0, 0),
                            });
                            
                            uvs.AddRange(new Vector2[] {
                                new Vector2(0, 0),
                                new Vector2(0, 1),
                                new Vector2(1, 0),
                                new Vector2(1, 1),
                            });
                            
                            triangles.AddRange(new int[] {
                                0 + triangleIndex, 1 + triangleIndex, 2 + triangleIndex,
                                2 + triangleIndex, 1 + triangleIndex, 3 + triangleIndex
                            });

                            triangleIndex += 4;
                        }
                        
                        // Top face
                        if (IsTransparent(x, y + 1, z, Voxels))
                        {
                            vertices.AddRange(new Vector3[] {
                                new Vector3(x - 0.5f, y + 0.5f, z - 0.5f),
                                new Vector3(x + 0.5f, y + 0.5f, z - 0.5f),
                                new Vector3(x - 0.5f, y + 0.5f, z + 0.5f),
                                new Vector3(x + 0.5f, y + 0.5f, z + 0.5f),
                            });

                            normals.AddRange(new Vector3[] {
                                new Vector3(0, 1, 0),
                                new Vector3(0, 1, 0),
                                new Vector3(0, 1, 0),
                                new Vector3(0, 1, 0),
                            });
                            tangents.AddRange(new Vector3[] {
                                new Vector3(0, 0, 1),
                                new Vector3(0, 0, 1),
                                new Vector3(0, 0, 1),
                                new Vector3(0, 0, 1),
                            });
                            
                            uvs.AddRange(new Vector2[] {
                                new Vector2(0, 0),
                                new Vector2(0, 1),
                                new Vector2(1, 0),
                                new Vector2(1, 1),
                            });
                            
                            triangles.AddRange(new int[] {
                                0 + triangleIndex, 1 + triangleIndex, 2 + triangleIndex,
                                2 + triangleIndex, 1 + triangleIndex, 3 + triangleIndex
                            });

                            triangleIndex += 4;
                        }
                        // Bottom face
                        if (IsTransparent(x, y - 1, z, Voxels))
                        {
                            vertices.AddRange(new Vector3[] {
                                new Vector3(x - 0.5f, y - 0.5f, z - 0.5f),
                                new Vector3(x + 0.5f, y - 0.5f, z - 0.5f),
                                new Vector3(x - 0.5f, y - 0.5f, z + 0.5f),
                                new Vector3(x + 0.5f, y - 0.5f, z + 0.5f),
                            });

                            normals.AddRange(new Vector3[] {
                                new Vector3(0, -1, 0),
                                new Vector3(0, -1, 0),
                                new Vector3(0, -1, 0),
                                new Vector3(0, -1, 0),
                            });
                            tangents.AddRange(new Vector3[] {
                                new Vector3(0, 0, -1),
                                new Vector3(0, 0, -1),
                                new Vector3(0, 0, -1),
                                new Vector3(0, 0, -1),
                            });
                            
                            uvs.AddRange(new Vector2[] {
                                new Vector2(1, 0),
                                new Vector2(1, 1),
                                new Vector2(0, 0),
                                new Vector2(0, 1),
                            });
                            
                            triangles.AddRange(new int[] {
                                0 + triangleIndex, 2 + triangleIndex, 1 + triangleIndex,
                                2 + triangleIndex, 3 + triangleIndex, 1 + triangleIndex
                            });

                            triangleIndex += 4;
                        }
                        
                        // Right face
                        if (IsTransparent(x + 1, y, z, Voxels))
                        {
                            vertices.AddRange(new Vector3[] {
                                new Vector3(x + 0.5f, y - 0.5f, z - 0.5f),
                                new Vector3(x + 0.5f, y + 0.5f, z - 0.5f),
                                new Vector3(x + 0.5f, y - 0.5f, z + 0.5f),
                                new Vector3(x + 0.5f, y + 0.5f, z + 0.5f),
                            });

                            normals.AddRange(new Vector3[] {
                                new Vector3(1, 0, 0),
                                new Vector3(1, 0, 0),
                                new Vector3(1, 0, 0),
                                new Vector3(1, 0, 0),
                            });
                            tangents.AddRange(new Vector3[] {
                                new Vector3(0, 0, -1),
                                new Vector3(0, 0, -1),
                                new Vector3(0, 0, -1),
                                new Vector3(0, 0, -1),
                            });
                            
                            uvs.AddRange(new Vector2[] {
                                new Vector2(1, 0),
                                new Vector2(1, 1),
                                new Vector2(0, 0),
                                new Vector2(0, 1),
                            });
                            
                            triangles.AddRange(new int[] {
                                0 + triangleIndex, 2 + triangleIndex, 1 + triangleIndex,
                                2 + triangleIndex, 3 + triangleIndex, 1 + triangleIndex
                            });

                            triangleIndex += 4;
                        }
                        // Left face
                        if (IsTransparent(x - 1, y, z, Voxels))
                        {
                            vertices.AddRange(new Vector3[] {
                                new Vector3(x - 0.5f, y - 0.5f, z - 0.5f),
                                new Vector3(x - 0.5f, y + 0.5f, z - 0.5f),
                                new Vector3(x - 0.5f, y - 0.5f, z + 0.5f),
                                new Vector3(x - 0.5f, y + 0.5f, z + 0.5f),
                            });

                            normals.AddRange(new Vector3[] {
                                new Vector3(-1, 0, 0),
                                new Vector3(-1, 0, 0),
                                new Vector3(-1, 0, 0),
                                new Vector3(-1, 0, 0),
                            });
                            tangents.AddRange(new Vector3[] {
                                new Vector3(0, 0, 1),
                                new Vector3(0, 0, 1),
                                new Vector3(0, 0, 1),
                                new Vector3(0, 0, 1),
                            });
                            
                            uvs.AddRange(new Vector2[] {
                                new Vector2(0, 0),
                                new Vector2(0, 1),
                                new Vector2(1, 0),
                                new Vector2(1, 1),
                            });
                            
                            triangles.AddRange(new int[] {
                                0 + triangleIndex, 1 + triangleIndex, 2 + triangleIndex,
                                2 + triangleIndex, 1 + triangleIndex, 3 + triangleIndex
                            });

                            triangleIndex += 4;
                        }
                        
                        #endregion
                    }
                }
            }

            IsRenderReady = true;
            MeshData opaqueData = new MeshData(
                true,
                vertices.ToArray(),
                normals.ToArray(),
                tangents.ToArray(),
                uvs.ToArray(),
                triangles.ToArray()
            );
            return new ChunkData(opaqueData);
        }

        IsRenderReady = true;
        return ChunkData.Empty;
    }

    private static bool IsTransparent(int x, int y, int z, uint[,,] voxels)
    {
        // Check if the coordinates are outside of the current chunk boundaries
        if (x < 0 || y < 0 || z < 0 || x >= ChunkSize || y >= ChunkSize || z >= ChunkSize)
        {
            return true;
            // return y <= 0;
        }
        else
        {
            // Inside current chunk
            return voxels[x, y, z] == 0;
        }
    }

    internal void Render(Camera camera)
    {
        if (IsRenderReady)
            _opaqueMesh.Render(camera);
    }

    struct ChunkData
    {
        public static ChunkData Empty => new(new MeshData(false, null, null, null, null, null));

        public MeshData opaqueData;

        public ChunkData(MeshData opaqueData)
        {
            this.opaqueData = opaqueData;
        }
    }

    struct MeshData
    {
        public bool hasData;
        public Vector3[] vertices;
        public Vector3[] normals;
        public Vector3[] tangents;
        public Vector2[] uvs;
        public int[] triangles;

        public MeshData(bool hasData, Vector3[] vertices, Vector3[] normals, Vector3[] tangents, Vector2[] uvs, int[] triangles)
        {
            this.hasData = hasData;
            this.vertices = vertices;
            this.normals = normals;
            this.tangents = tangents;
            this.uvs = uvs;
            this.triangles = triangles;
        }
    }
}