using System.Text;

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
        Glyph glyph = reader.ReadSimpleGlyph();
        Console.WriteLine(glyph.ToString());
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

    private Glyph ReadSimpleGlyph()
    {
        Glyph glyph = new Glyph();
        glyph.Header = ReadGlyphHeader();
        glyph.EndPtsOfContours = ReadUInt16Array(glyph.Header.NumContours);
        glyph.NumPoints = (UInt16)(glyph.EndPtsOfContours[^1] + 1);
        glyph.InstructionLength = ReadUInt16();
        glyph.Instructions = ReadByteArray(glyph.InstructionLength);

        var flags = new Byte[glyph.NumPoints];
        for (int i = 0; i < glyph.NumPoints; i++)
        {
            Byte flag = reader.ReadByte();
            flags[i] = flag;

            // check repeat bit, if it is on, the next byte determines how many times this flag repeats
            if (FlagBitSet(flag, 3))
            {
                int repeatCount = reader.ReadByte();
                for (int r = 0; r < repeatCount; r++)
                {
                    flags[++i] = flag;
                }
            }
        }

        glyph.XCoordinates = ReadCoordinates(flags, readingX: true);
        glyph.YCoordinates = ReadCoordinates(flags, readingX: false);

        glyph.Flags = flags;

        return glyph;

        Int16[] ReadCoordinates(Byte[] allFlags, bool readingX)
        {
            int offsetSizeFlagBit = readingX ? 1 : 2;
            int signFlagBit = readingX ? 4 : 5;
            var coordinates = new Int16[allFlags.Length];
            Int16 currentCoordinate = 0;

            for (int i = 0; i < coordinates.Length; i++)
            {
                Byte flag = allFlags[i];
                Console.WriteLine($"Flag {i}: {Convert.ToString(flag, 2).PadLeft(8, '0')}");

                if (FlagBitSet(flag, offsetSizeFlagBit))
                {
                    Byte offset = reader.ReadByte();
                    Console.WriteLine($"Offset (byte) {i}: {offset}");

                    if (FlagBitSet(flag, signFlagBit))
                    {
                        currentCoordinate += offset;
                    }
                    else
                    {
                        currentCoordinate -= offset;
                    }
                }
                else if (!FlagBitSet(flag, signFlagBit))
                {
                    Int16 offset = ReadInt16();
                    Console.WriteLine($"Offset (short) {i}: {offset}");
                    currentCoordinate += offset;
                }
                // Implicit handling when neither sizeFlag nor signFlag are set (coordinate remains the same)

                coordinates[i] = currentCoordinate;
                Console.WriteLine($"Coordinate {i}: {currentCoordinate}");
            }

            return coordinates;
        }
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

    private bool FlagBitSet(byte flag, int bitIndex)
    {
        return (flag & (1 << bitIndex)) != 0;
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

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Glyph {{");
            sb.AppendLine($"\tNum Contours: {Header.NumContours} Num Points: {NumPoints}\n");

            for (int i = 0; i < Header.NumContours; i++)
            {
                sb.AppendLine($"\tContour {i} end index: {EndPtsOfContours[i]}");
            }

            sb.AppendLine("");
            for (int i = 0; i < NumPoints; i++)
            {
                sb.AppendLine($"\tPoint {i}: (X: {XCoordinates[i]} Y: {YCoordinates[i]})");
            }

            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
