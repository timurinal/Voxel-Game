using System.Runtime.InteropServices;
using OpenTK.Mathematics;
using VoxelGame;
using VoxelGame.Graphics.Shaders;
using VoxelGame.Maths;
using VoxelGame.TerrainGeneration;
using Vector2 = VoxelGame.Maths.Vector2;
using Vector3 = VoxelGame.Maths.Vector3;

namespace VoxelGame.Graphics;

internal class Chunk : IDisposable
{
    public const int ChunkSize = 16;
    public const int HChunkSize = ChunkSize / 2;
    public const int ChunkArea = ChunkSize * ChunkSize;
    public const int ChunkVolume = ChunkArea * ChunkSize;

    public Vector3Int Position;
    /// Position in chunk-space
    public Vector3Int CSPosition;
    public Vector3 Centre =>
        new(Position.X + HChunkSize, 
            Position.Y + HChunkSize,
            Position.Z + HChunkSize);

    public AABB Bounds;

    public int SolidVoxelCount;
    public bool IsEmpty => SolidVoxelCount <= 0;
    
    public bool IsRenderReady { get; private set; }
    
    public uint[] Voxels;

    private Material chunkMaterial;
    private int opaqueMesh_vao;
    private int opaqueMesh_vbo;
    private int opaqueMesh_ebo;
    private int _vertexCount, _triangleCount;
    
    // private Mesh _opaqueMesh;

    public Chunk(Vector3Int position, Material material)
    {
        Position = position * ChunkSize;
        CSPosition = position;
        
        Bounds = AABB.CreateFromExtents(Centre, Vector3.One * HChunkSize);
        
        Voxels = new uint[ChunkVolume];
        
        SolidVoxelCount = 0;

        Parallel.For(0, ChunkVolume, i =>
        {
            int x = i % ChunkSize;
            int y = (i / ChunkSize) % ChunkSize;
            int z = i / ChunkArea;
            
            int globalX = x + Position.X;
            int globalY = y + Position.Y;
            int globalZ = z + Position.Z;
                    
            uint vox = TerrainGenerator.SampleTerrain(globalX, globalY, globalZ);
            Voxels[i] = vox;

            SolidVoxelCount += vox != 0u ? 1 : 0;
        });

        // _opaqueMesh = new Mesh(material);
        // _opaqueMesh.Transform.Position = Position;

        chunkMaterial = material;
        opaqueMesh_vao = GL.GenVertexArray();
        opaqueMesh_vbo = GL.GenBuffer();
        opaqueMesh_ebo = GL.GenBuffer();
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
                GL.BindVertexArray(opaqueMesh_vao);
                
                GL.BindBuffer(BufferTarget.ArrayBuffer, opaqueMesh_vbo);
                GL.BufferData(BufferTarget.ArrayBuffer, 
                    data.opaqueData.data.Length * sizeof(UInt32), 
                    data.opaqueData.data, 
                    BufferUsageHint.DynamicDraw);
                
                GL.VertexAttribIPointer(0, 1, VertexAttribIntegerType.UnsignedInt, 0, 0);
                GL.EnableVertexAttribArray(0);
                
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, opaqueMesh_ebo);
                GL.BufferData(BufferTarget.ElementArrayBuffer, data.opaqueData.triangles.Length * sizeof(int), data.opaqueData.triangles, BufferUsageHint.DynamicDraw);
                
                GL.BindVertexArray(0);
                
                IsRenderReady = true;
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
            List<(byte x, byte y, byte z)> vertices = new();
            List<byte> normals = new();
            List<byte> textures = new();
            List<(bool u, bool v)> uvs = new();
            List<int> triangles = new();
            
            for (int i = 0, triangleIndex = 0; i < ChunkVolume; i++)
            {
                int x = i % ChunkSize;
                int y = (i / ChunkSize) % ChunkSize;
                int z = i / ChunkArea;
                
                byte bX = (byte)x;
                byte bY = (byte)y;
                byte bZ = (byte)z;
                
                uint voxel = Voxels[i];

                // 0 means air, so skip adding any faces for this voxel
                if (voxel == 0)
                    continue;
                
                byte frontTex = (byte)VoxelData.GetTextureFace(voxel - 1, VoxelFace.Front);
                byte backTex  = (byte)VoxelData.GetTextureFace(voxel - 1, VoxelFace.Back);
                byte upTex    = (byte)VoxelData.GetTextureFace(voxel - 1, VoxelFace.Top);
                byte downTex  = (byte)VoxelData.GetTextureFace(voxel - 1, VoxelFace.Bottom);
                byte rightTex = (byte)VoxelData.GetTextureFace(voxel - 1, VoxelFace.Right);
                byte leftTex  = (byte)VoxelData.GetTextureFace(voxel - 1, VoxelFace.Left);
                
                #region Chunk Builder
                
                // Front face
                if (IsTransparent(x, y, z - 1, Voxels, Engine.Chunks, CSPosition))
                {
                    vertices.AddRange([
                        new(bX        , bY        , bZ),
                        new(bX        , Add(bY, 1), bZ),
                        new(Add(bX, 1), bY        , bZ),
                        new(Add(bX, 1), Add(bY, 1), bZ),
                    ]);

                    // TODO: Profile performance difference between List.Add and List.AddRange
                    normals.AddRange([0, 0, 0, 0]);

                    textures.AddRange([ frontTex, frontTex, frontTex, frontTex ]);
                    
                    uvs.AddRange([ (true, false), (true, true), (false, false), (false, true) ]);
                    
                    triangles.AddRange(new int[] {
                        0 + triangleIndex, 2 + triangleIndex, 1 + triangleIndex,
                        2 + triangleIndex, 3 + triangleIndex, 1 + triangleIndex
                    });

                    triangleIndex += 4;
                }
                // Back face
                if (IsTransparent(x, y, z + 1, Voxels, Engine.Chunks, CSPosition))
                {
                    vertices.AddRange([
                        new(bX        , bY        , Add(bZ, 1)),
                        new(bX        , Add(bY, 1), Add(bZ, 1)),
                        new(Add(bX, 1), bY        , Add(bZ, 1)),
                        new(Add(bX, 1), Add(bY, 1), Add(bZ, 1)),
                    ]);

                    normals.AddRange([ 1, 1, 1, 1 ]);
                    
                    textures.AddRange([ backTex, backTex, backTex, backTex ]);
                    
                    uvs.AddRange([ (false, false), (false, true), (true, false), (true, true) ]);
                    
                    triangles.AddRange(new int[] {
                        0 + triangleIndex, 1 + triangleIndex, 2 + triangleIndex,
                        2 + triangleIndex, 1 + triangleIndex, 3 + triangleIndex
                    });

                    triangleIndex += 4;
                }
                
                // Top face
                if (IsTransparent(x, y + 1, z, Voxels, Engine.Chunks, CSPosition))
                {
                    vertices.AddRange([
                        new(bX        , Add(bY, 1), bZ        ),
                        new(Add(bX, 1), Add(bY, 1), bZ        ),
                        new(bX        , Add(bY, 1), Add(bZ, 1)),
                        new(Add(bX, 1), Add(bY, 1), Add(bZ, 1)),
                    ]);

                    normals.AddRange([ 2, 2, 2, 2 ]);
                    
                    textures.AddRange([ upTex, upTex, upTex, upTex ]);
                    
                    uvs.AddRange([ (false, false), (false, true), (true, false), (true, true) ]);
                    
                    triangles.AddRange(new int[] {
                        0 + triangleIndex, 1 + triangleIndex, 2 + triangleIndex,
                        2 + triangleIndex, 1 + triangleIndex, 3 + triangleIndex
                    });

                    triangleIndex += 4;
                }
                // Bottom face
                if (IsTransparent(x, y - 1, z, Voxels, Engine.Chunks, CSPosition))
                {
                    vertices.AddRange([
                        new(bX        , bY, bZ        ),
                        new(Add(bX, 1), bY, bZ        ),
                        new(bX        , bY, Add(bZ, 1)),
                        new(Add(bX, 1), bY, Add(bZ, 1)),
                    ]);

                    normals.AddRange([ 3, 3, 3, 3 ]);
                    
                    textures.AddRange([ downTex, downTex, downTex, downTex ]);
                    
                    uvs.AddRange([ (true, false), (true, true), (false, false), (false, true) ]);
                    
                    triangles.AddRange(new int[] {
                        0 + triangleIndex, 2 + triangleIndex, 1 + triangleIndex,
                        2 + triangleIndex, 3 + triangleIndex, 1 + triangleIndex
                    });

                    triangleIndex += 4;
                }
                
                // Right face
                if (IsTransparent(x + 1, y, z, Voxels, Engine.Chunks, CSPosition))
                {
                    vertices.AddRange([
                        new(Add(bX, 1), bY        , bZ        ),
                        new(Add(bX, 1), Add(bY, 1), bZ        ),
                        new(Add(bX, 1), bY        , Add(bZ, 1)),
                        new(Add(bX, 1), Add(bY, 1), Add(bZ, 1)),
                    ]);

                    normals.AddRange([ 4, 4, 4, 4 ]);
                    
                    textures.AddRange([ rightTex, rightTex, rightTex, rightTex ]);
                    
                    uvs.AddRange([ (true, false), (true, true), (false, false), (false, true) ]);
                    
                    triangles.AddRange(new int[] {
                        0 + triangleIndex, 2 + triangleIndex, 1 + triangleIndex,
                        2 + triangleIndex, 3 + triangleIndex, 1 + triangleIndex
                    });

                    triangleIndex += 4;
                }
                // Left face
                if (IsTransparent(x - 1, y, z, Voxels, Engine.Chunks, CSPosition))
                {
                    vertices.AddRange([
                        new(bX, bY        , bZ        ),
                        new(bX, Add(bY, 1), bZ        ),
                        new(bX, bY        , Add(bZ, 1)),
                        new(bX, Add(bY, 1), Add(bZ, 1)),
                    ]);

                    normals.AddRange([ 5, 5, 5, 5 ]);
                    
                    textures.AddRange([ leftTex, leftTex, leftTex, leftTex ]);
                    
                    uvs.AddRange([ (false, false), (false, true), (true, false), (true, true) ]);
                    
                    triangles.AddRange(new int[] {
                        0 + triangleIndex, 1 + triangleIndex, 2 + triangleIndex,
                        2 + triangleIndex, 1 + triangleIndex, 3 + triangleIndex
                    });

                    triangleIndex += 4;
                }
                
                #endregion
            }

            UInt32[] data = new uint[vertices.Count];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = PackData(
                    vertices[i].x, vertices[i].y, vertices[i].z, normals[i], textures[i], uvs[i].u, uvs[i].v
                );
            }

            _triangleCount = triangles.Count;
            _vertexCount = vertices.Count;
            
            MeshData opaqueData = new MeshData(
                true,
                data,
                triangles.ToArray()
            );
            return new ChunkData(opaqueData);
        }

        IsRenderReady = false;
        return ChunkData.Empty;
    }

    public static bool IsTransparent(int x, int y, int z, uint[] voxels, Dictionary<Vector3Int, Chunk> chunks, Vector3Int currentChunkPosition)
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
                return neighborChunk.Voxels[index] == 0u;
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

    internal void Render(bool bindShader = true)
    {
        if (IsRenderReady)
        {
            if (bindShader)
                chunkMaterial.Use();
            
            chunkMaterial.Shader.SetVector3("chunkPosition", Position, autoUse: false);
            
            // Bind the vertex array
            GL.BindVertexArray(opaqueMesh_vao);
            GL.DrawElements(PrimitiveType.Triangles, _triangleCount, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);

            Engine.TriangleCount += _triangleCount / 3;
            Engine.VertexCount += _vertexCount;
        }
    }

    /// <summary>
    /// Renders the chunk using a sun VP matrix
    /// </summary>
    internal void DirLightRender(Shader shader, DirectionalLight light)
    {
        if (IsRenderReady)
        {
            shader.SetVector3("chunkPosition", Position, autoUse: false);
            
            // Bind the vertex array
            GL.BindVertexArray(opaqueMesh_vao);
            GL.DrawElements(PrimitiveType.Triangles, _triangleCount, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }
    }
    
    public static int FlattenIndex3D(int x, int y, int z, int width, int height)
    {
        return x + (y * width) + (z * width * height);
    }

    private static UInt32 PackData(byte x, byte y, byte z, byte face, byte texture, bool texU, bool texV)
    {
        if (x > 63 || y > 63 || z > 63) throw new ArgumentException("Positions can only be in the range 0-63");
        if (face > 7) throw new ArgumentException("Face ID can only be in the range 0-7");

        UInt32 packedData = 0;

        // Data is layed out as follows:
        // 0 U V TTTTTTTT FFF ZZZZZZ YYYYYY XXXXXX
        packedData |= ((UInt32)(texU ? 1 : 0) << 31); // U - 1 bit is for uv U
        packedData |= ((UInt32)(texV ? 1 : 0) << 30); // V - 1 bit is for uv V
        packedData |= ((UInt32)texture << 22); // TTTTTTTT - 8 bits are for texture
        packedData |= ((UInt32)face << 19); // FFF - 3 bits for face
        packedData |= ((UInt32)z << 13); // ZZZZZZ - 6 bits are for z
        packedData |= ((UInt32)y << 7); // YYYYYY - 6 bits are for y
        packedData |= x; // XXXXXX - 6 bits are for x

        return packedData;
    }

    private static byte Add(byte a, byte b)
    {
        return (byte)(a + b);
    }

    struct ChunkData
    {
        public static ChunkData Empty => new(new MeshData(false, null, null));

        public MeshData opaqueData;

        public ChunkData(MeshData opaqueData)
        {
            this.opaqueData = opaqueData;
        }
    }

    struct MeshData
    {
        public bool hasData;
        public UInt32[] data;
        public int[] triangles;

        public MeshData(bool hasData, UInt32[] data, int[] triangles)
        {
            this.hasData = hasData;
            this.data = data;
            this.triangles = triangles;
        }
    }

    public void Dispose()
    {
        GL.DeleteVertexArray(opaqueMesh_vao);
        GL.DeleteBuffer(opaqueMesh_vbo);
        GL.DeleteBuffer(opaqueMesh_ebo);
    }
}