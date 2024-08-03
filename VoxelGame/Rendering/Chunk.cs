using VoxelGame.Maths;
using VoxelGame.TerrainGeneration;

namespace VoxelGame.Rendering;

public class Chunk
{
    public const int ChunkSize = 8;
    public const int HChunkSize = ChunkSize / 2;
    public const int ChunkArea = ChunkSize * ChunkSize;
    public const int ChunkVolume = ChunkArea * ChunkSize;

    public Vector3Int Position;
    public Vector3 Centre =>
        new(Position.X + HChunkSize, Position.Y + HChunkSize,
            Position.Z + HChunkSize);

    public AABB Bounds;
    
    public uint[,,] Voxels;

    public int SolidVoxelCount;

    private Mesh _chunkMesh;

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
                    float val = TerrainGenerator.Sample(x + Position.X + 20, y + Position.Y + 4, z + Position.Z, scale: 0.1f);
                    
                    uint vox = val >= 0.5f ? 1u : 0u;
                    Voxels[x, y, z] = vox;

                    SolidVoxelCount += vox != 0u ? 1 : 0;
                }
            }
        }
    }

    internal Engine.RTCube[] GenCubes(Engine.RTMaterial mat)
    {
        List<Engine.RTCube> cubes = new();
        
        for (int x = 0; x < ChunkSize; x++)
        {
            for (int y = 0; y < ChunkSize; y++)
            {
                for (int z = 0; z < ChunkSize; z++)
                {
                    if (Voxels[x, y, z] != 0u)
                        cubes.Add(new Engine.RTCube(new Vector3(x + Position.X, y + Position.Y, z + Position.Z), 0, mat));
                }
            }
        }

        return cubes.ToArray();
    }
}