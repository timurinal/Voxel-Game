using Newtonsoft.Json;
using VoxelGame.Maths;

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

    internal static int GetTextureFace(uint voxelId, VoxelFace face) => Voxels[voxelId].textureFaces[(int)face];
    
    /// <summary>
    /// Converts a voxel name to its corresponding voxel ID.
    /// </summary>
    /// <param name="name">The name of the voxel.</param>
    /// <returns>The voxel ID.</returns>
    /// <remarks>Returns 0 if the name is not found</remarks>
    public static uint NameToVoxelId(string name)
    {
        if (name == "air") return 0;
        
        foreach (var voxel in Voxels)
        {
            if (voxel.name == name) return voxel.id;
        }

        return 0;
    }

    /// <summary>
    /// Converts a voxel ID to its corresponding voxel name.
    /// </summary>
    /// <param name="id">The ID of the voxel.</param>
    /// <returns>The voxel name.</returns>
    /// <remarks>Returns 'air' if the voxel id is not found</remarks>
    public static string VoxelIdToName(uint id)
    {
        if (id == 0) return "air";
        
        foreach (var voxel in Voxels)
        {
            if (voxel.id == id) return voxel.name;
        }

        return "air";
    }
    
    public struct Voxel
    {
        public uint id;
        public string displayName;
        public string name;
        public int[] textureFaces;
        
        public Vector3 lightColour;
        public float lightRange;
        
        [JsonConstructor]
        public Voxel(uint id, string displayName, string name, int[] textureFaces, float[] lightColour, float lightRange)
        {
            if (textureFaces.Length > 6) throw new ArgumentException("A voxel can't have more than 6 textures!");
            if (lightColour != null && lightColour.Length != 3) throw new ArgumentException("Light colour needs 3 colour properties!");
            
            this.id = id;
            this.displayName = displayName;
            this.name = name;
            this.textureFaces = textureFaces;

            if (lightColour != null)
                this.lightColour = new Vector3(lightColour[0], lightColour[1], lightColour[2]);
            else
                this.lightColour = Vector3.Zero;
            
            this.lightRange = lightRange != null ? lightRange : 0f;
        }

        public override string ToString()
        {
            var idString = $"ID: {id}";
            var displayNameString = $"Display Name: \"{displayName}\"";
            var nameString = $"Name: \"{name}\"";
            var textureFacesString = $"Texture Faces: [ {string.Join(", ", textureFaces)} ]";
            var lightColourString = $"Light Colour: {lightColour}";
            var lightRangeString = $"Light Range: {lightRange}";

            var voxelDataString =
                $"\"{displayName}\" Voxel Data: {{\n\t{idString}\n\t{displayNameString}\n\t{nameString}\n\t{textureFacesString}\n\t{lightColourString}\n\t{lightRangeString}\n}}";

            return voxelDataString;
        }
    }
}