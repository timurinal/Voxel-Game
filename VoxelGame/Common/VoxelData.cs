using Newtonsoft.Json;
using VoxelGame.Maths;

namespace VoxelGame;

internal enum VoxelFace
{
    Front,
    Back,
    Top,
    Bottom,
    Right,
    Left
}

public static class VoxelData
{
    public static Voxel[] Voxels;

    public const string VoxelDataLocation = "VoxelGame.assets.voxels.json";

    public const uint AirVoxelId = 0u;
    
    static VoxelData()
    {
        if (Environment.LoadAssemblyStream(VoxelDataLocation, out var stream))
        {
            using var streamReader = new StreamReader(stream);
            var serializer = new JsonSerializer();
            Voxels = (Voxel[])serializer.Deserialize(streamReader, typeof(Voxel[]));
        }
        else
        {
            throw new Exception($"Couldn't find voxel data file at location {VoxelDataLocation}");
        }
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

    public static bool IsTransparent(uint voxelId)
    {
        if (voxelId == 0)
            return true;

        foreach (var voxel in Voxels)
        {
            if (voxel.id == voxelId)
            {
                return voxel.transparent;
            }
        }

        return true;
    }
    
    public static bool IsTransparent(uint voxelId, uint selfVoxelId)
    {
        if (voxelId == 0)
            return true;

        bool isVoxelTransparent = IsTransparent(voxelId);
        bool isSelfTransparent = IsTransparent(selfVoxelId);
        
        foreach (var voxel in Voxels)
        {
            if (voxel.id == voxelId || isVoxelTransparent == isSelfTransparent)
            {
                return voxel.transparent;
            }
        }

        return true;
    }

    public static bool IsLiquid(uint voxelId)
    {
        if (voxelId == 0)
            return false;

        foreach (var voxel in Voxels)
        {
            if (voxel.id == voxelId)
                return voxel.liquid;
        }

        return false;
    }
    
    public struct Voxel
    {
        public uint id;
        public string displayName;
        public string name;
        public int[] textureFaces;
        public bool transparent;
        public bool liquid;
        
        public Vector3 lightColour;
        public float lightRange;
        
        [JsonConstructor]
        public Voxel(uint id, string displayName, string name, int[] textureFaces, bool transparent, bool liquid, float[] lightColour, float lightRange)
        {
            if (textureFaces.Length > 6) throw new ArgumentException("A voxel can't have more than 6 textures!");
            if (lightColour != null && lightColour.Length != 3) throw new ArgumentException("Light colour needs 3 colour properties!");
            
            this.id = id;
            this.displayName = displayName;
            this.name = name;
            this.textureFaces = textureFaces;

            this.transparent = transparent != null ? transparent : false;
            this.liquid = liquid != null ? liquid : false;
            
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
            var transparentString = $"Transparent: {transparent}";
            var lightColourString = $"Light Colour: {lightColour}";
            var lightRangeString = $"Light Range: {lightRange}";

            var voxelDataString =
                $"\"{displayName}\" Voxel Data: {{\n\t{idString}\n\t{displayNameString}\n\t{nameString}\n\t{textureFacesString}\n\t{transparentString}\n\t{lightColourString}\n\t{lightRangeString}\n}}";

            return voxelDataString;
        }
    }
}