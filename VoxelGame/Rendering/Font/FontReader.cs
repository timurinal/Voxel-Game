namespace VoxelGame.Rendering.Font;

public sealed class FontReader : IDisposable
{
    public readonly Stream stream;
    public readonly BinaryReader reader;
    
    public FontReader(string fontPath)
    {
        if (!fontPath.EndsWith(".ttf")) throw new ArgumentException("Font file must be a TrueType font file!");
        
        stream = File.Open(fontPath, FileMode.Open);
        reader = new BinaryReader(stream);
    }

    public static void ParseFont(string fontPath)
    {
        using FontReader reader = new FontReader(fontPath);
        
        reader.SkipBytes(4); // skip 4 byte scalar type
        UInt16 numTables = reader.ReadUInt16();
        reader.SkipBytes(6);
        
        // Table directory
        Dictionary<string, uint> tableLocationLookup = new();
        for (int i = 0; i < numTables; i++)
        {
            string tag = reader.ReadTag();
            uint checksum = reader.ReadUInt32();
            uint offset = reader.ReadUInt32();
            uint length = reader.ReadUInt32();
            
            tableLocationLookup.Add(tag, offset);
        }
        
        reader.Goto(tableLocationLookup["glyf"]);
        Glyph glyph = reader.ReadGlyph();
        Console.WriteLine(glyph.NumPoints);
        reader.SkipBytes(glyph.GetSize());
        glyph = reader.ReadGlyph();
        Console.WriteLine(glyph.NumPoints);
    }

    private GlyphHeader ReadGlyphHeader()
    {
        GlyphHeader header = new GlyphHeader
        {
            NumContours = ReadFWord(),
            XMin        = ReadFWord(),
            YMin        = ReadFWord(),
            XMax        = ReadFWord(),
            YMax        = ReadFWord()
        };

        return header;
    }

    private Glyph ReadGlyph()
    {
        Glyph glyph = new Glyph
        {
            Header = ReadGlyphHeader()
        };

        glyph.EndPtsOfContours = ReadUInt16Array(glyph.Header.NumContours);
        glyph.NumPoints = (UInt16)(glyph.EndPtsOfContours[^1] + 1);
        glyph.InstructionLength = ReadUInt16();
        glyph.Instructions = ReadByteArray(glyph.InstructionLength);
        // TODO: flags and positions
        return glyph;
    }

    private UInt16[] ReadUInt16Array(int len)
    {
        Span<UInt16> arr = stackalloc ushort[len];
        for (int i = 0; i < len; i++)
        {
            arr[i] = ReadUInt16();
        }
        return arr.ToArray();
    }
    
    private Byte[] ReadByteArray(int len)
    {
        Span<Byte> arr = stackalloc byte[len];
        for (int i = 0; i < len; i++)
        {
            arr[i] = reader.ReadByte();
        }
        return arr.ToArray();
    }

    private UInt16 ReadUInt16()
    {
        UInt16 value = reader.ReadUInt16();

        if (BitConverter.IsLittleEndian)
        {
            value = (UInt16)(value >> 8 | value << 8);
        }

        return value;
    }
    
    private Int16 ReadInt16()
    {
        Int16 value = reader.ReadInt16();

        if (BitConverter.IsLittleEndian)
        {
            value = (Int16)(value >> 8 | value << 8);
        }

        return value;
    }

    private Int16 ReadFWord() => ReadInt16();

    private UInt32 ReadUInt32()
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

    private string ReadTag()
    {
        Span<char> tag = stackalloc char[4];

        for (int i = 0; i < tag.Length; i++)
            tag[i] = (char)reader.ReadByte();

        return tag.ToString();
    }

    private void SkipBytes(uint bytes) => stream.Position += bytes;
    private void Goto(uint bytePosition) => stream.Position = bytePosition;
    
    public void Dispose()
    {
        stream.Close();
        reader.Close();
    }

    struct GlyphHeader
    {
        public Int16 NumContours;
        public Int16 XMin;
        public Int16 YMin;
        public Int16 XMax;
        public Int16 YMax;
    }
    struct Glyph
    {
        public GlyphHeader Header;
        public UInt16[] EndPtsOfContours;
        public UInt16 NumPoints;
        public UInt16 InstructionLength;
        public Byte[] Instructions;
        public Byte[] Flags;
        public Int16[] XCoordinates, YCoordinates;

        public uint GetSize()
        {
            int size = 0;

            //Size of GlyphHeader is fixed
            size += sizeof(Int16) * 5; //GlyphHeader consists of 5 Int16 types.

            //Accounting arrays' sizes, considering that each item occupies memory in bytes equals to sizeof(type)
            size += (this.EndPtsOfContours?.Length ?? 0) * sizeof(UInt16);
            size += sizeof(UInt16); //NumPoints
            size += sizeof(UInt16); //InstructionLength
            size += (this.Instructions?.Length ?? 0) * sizeof(Byte);
            size += (this.Flags?.Length ?? 0) * sizeof(Byte);
            size += (this.XCoordinates?.Length ?? 0) * sizeof(Int16);
            size += (this.YCoordinates?.Length ?? 0) * sizeof(Int16);

            return (uint)size;
        }
    }
}