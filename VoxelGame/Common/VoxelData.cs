using Newtonsoft.Json;

namespace VoxelGame;

public static class VoxelData
{
    public static Voxel[] Voxels;

    public const string VoxelDataPath = "Assets/voxels.json";
    
    static VoxelData()
    {
        using var fileReader = File.OpenText(VoxelDataPath);
        var serializer = new JsonSerializer();
        Voxels = (Voxel[])serializer.Deserialize(fileReader, typeof(Voxel[]));
    }
    
    public struct Voxel
    {
        public uint id;
        public string displayName;
        public string name;
        public int[] textureFaces;

        public Voxel(uint id, string displayName, string name, int[] textureFaces)
        {
            if (textureFaces.Length > 6) throw new ArgumentException("A voxel can't have more than 6 textures!");
            
            this.id = id;
            this.displayName = displayName;
            this.name = name;
            this.textureFaces = textureFaces;
        }

        public override string ToString()
        {
            var idString = $"ID: {id}";
            var displayNameString = $"Display Name: \"{displayName}\"";
            var nameString = $"Name: \"{name}\"";
            var textureFacesString = $"Texture Faces: [ {string.Join(", ", textureFaces)} ]";

            var voxelDataString =
                $"Voxel Data: {{\n\t{idString}\n\t{displayNameString}\n\t{nameString}\n\t{textureFacesString}\n}}";

            return voxelDataString;
        }
    }
}