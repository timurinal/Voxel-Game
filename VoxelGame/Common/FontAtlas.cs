using Newtonsoft.Json;
using VoxelGame.Maths;
using VoxelGame.Rendering;

namespace VoxelGame;

public static class FontAtlas
{
    public const int FontAtlasWidth = 128, FontAtlasHeight = 128;
    public const int CharacterSize = 8;
    public const string FontAtlasPath = "VoxelGame.Assets.Textures.atlas-font.png";
    public const string CharacterSetLocation = "VoxelGame.Assets.character-set.json";
    
    public static Texture2D FontTexture { get; private set; }

    public static CharacterSet MainCharacterSet;

    internal static void Init()
    {
        FontTexture = Texture2D.LoadFromAssembly(FontAtlasPath, false, false, false);

        MainCharacterSet = new CharacterSet(CharacterSetLocation);
    }
    
    internal static Vector2 GetUVForFont(int fontId, int u, int v)
    {
        int texturePerRow = FontAtlasWidth / CharacterSize;
        float unit = 1.0f / texturePerRow;

        float x = (fontId % texturePerRow) * unit;
        float y = (fontId / texturePerRow) * unit;

        return new Vector2(x + u * unit, y + v * unit);
    }

    public struct CharacterSet
    {
        public Character[] Characters;
        
        public static Character MissingCharacter => new('\u0000', 0, 5);

        public CharacterSet(string location)
        {
            
            if (Environment.LoadAssemblyStream(location, out var stream))
            {
                using var streamReader = new StreamReader(stream);
                var serializer = new JsonSerializer();
                Characters = (Character[])serializer.Deserialize(streamReader, typeof(Character[]));
            }
            else
            {
                throw new Exception($"Couldn't find character data file at location {location}");
            }
        }

        public Character GetCharacter(char character)
        {
            for (int i = 0; i < Characters.Length; i++)
            {
                if (Characters[i].Char == character)
                {
                    return Characters[i];
                }
            }

            return MissingCharacter;
        }

        public struct Character
        {
            [JsonProperty("character")] public char Char;
            public int Id;
            public int Width;

            [System.Text.Json.Serialization.JsonConstructor]
            public Character(char character, int id, int width)
            {
                Char = character;
                Id = id;
                Width = width;
            }

            public override string ToString()
            {
                return @$"
{{
    Character: {Char},
    ID: {Id},
    Width (px): {Width}
}}
";
            }
        }
    }
}