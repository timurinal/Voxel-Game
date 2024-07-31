using VoxelGame.Maths;

namespace VoxelGame.Rendering;

public class Chunk
{
    public const int ChunkSizeX = 16;
    public const int ChunkSizeY = 256;
    public const int ChunkSizeZ = 16;

    public const int ChunkVolume = ChunkSizeX * ChunkSizeY * ChunkSizeZ;

    public uint[,,] Voxels;

    private Mesh _chunkMesh;

    public Chunk(Vector3Int position, Material material)
    {
        Voxels = new uint[ChunkSizeX, ChunkSizeY, ChunkSizeZ];
        
        for (int x = 0; x < ChunkSizeX; x++)
        {
            for (int y = 0; y < ChunkSizeY; y++)
            {
                for (int z = 0; z < ChunkSizeZ; z++)
                {
                    Voxels[x, y, z] = 1;
                }
            }
        }

        _chunkMesh = new Mesh(material);
    }
}