namespace VoxelGame.Rendering.Font;

public sealed class FontReader : IDisposable
{
    public readonly Stream stream;
    public readonly BinaryReader reader;
    
    public FontReader(string fontPath)
    {
        stream = File.Open(fontPath, FileMode.Open);
        reader = new BinaryReader(stream);
    }

    public static void ParseFont(string fontPath)
    {
        using FontReader reader = new FontReader(fontPath);
        
        reader.SkipBytes(4); // skip 4 byte scalar type
        UInt16 numTables = reader.ReadUInt16();
        reader.SkipBytes(6);
        Console.WriteLine($"NumTables: {numTables}");
        
        // Table directory
        for (int i = 0; i < numTables; i++)
        {
            string tag = reader.ReadTag();
            uint checksum = reader.ReadUInt32();
            uint offset = reader.ReadUInt32();
            uint length = reader.ReadUInt32();
            Console.WriteLine($"Tag: {tag} Location: {offset}");
        }
    }

    public UInt16 ReadUInt16()
    {
        UInt16 value = reader.ReadUInt16();

        if (BitConverter.IsLittleEndian)
        {
            value = (UInt16)(value >> 8 | value << 8);
        }

        return value;
    }

    public UInt32 ReadUInt32()
    {
        UInt32 value = reader.ReadUInt32();

        if (BitConverter.IsLittleEndian)
        {
            const byte ByteMask = 0b11111111;
            UInt32 a = (value >> 24) & ByteMask;
            UInt32 b = (value >> 16) & ByteMask;
            UInt32 c = (value >>  8) & ByteMask;
            UInt32 d = (value >>  0) & ByteMask;
            value = a << 0 | b << 8 | c << 16 | d << 24;
        }

        return value;
    }

    public string ReadTag()
    {
        Span<char> tag = stackalloc char[4];

        for (int i = 0; i < tag.Length; i++)
            tag[i] = (char)reader.ReadByte();

        return tag.ToString();
    }

    public void SkipBytes(uint bytes) => stream.Position += bytes;
    
    public void Dispose()
    {
        stream.Dispose();
        reader.Dispose();
    }
}