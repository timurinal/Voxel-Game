using OpenTK.Mathematics;
using VoxelGame.Maths;

namespace VoxelGame.Graphics;

public struct Colour
{
    public float R { get; set; }
    public float G { get; set; }
    public float B { get; set; }
    public float A { get; set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Colour"/> struct.
    /// </summary>
    /// <param name="r">The red component of the colour, ranging from 0 (none) to 1 (full intensity).</param>
    /// <param name="g">The green component of the colour, ranging from 0 (none) to 1 (full intensity).</param>
    /// <param name="b">The blue component of the colour, ranging from 0 (none) to 1 (full intensity).</param>
    /// <param name="a">The alpha (transparency) component of the colour, ranging from 0 (fully transparent) to 1 (fully opaque).</param>
    public Colour(float r = 0, float g = 0, float b = 0, float a = 0.5f)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public static Colour Red => new(1f, 0f, 0f);
    public static Colour Green => new(0f, 1f, 0f);
    public static Colour Blue => new(0f, 0f, 1f);
    
    public static Colour Yellow => new(1f, 1f, 0f);
    public static Colour Cyan => new(0f, 1f, 1f);
    public static Colour Magenta => new(1f, 0f, 1f);
    
    public static Colour White => new(1f, 1f, 1f);
    public static Colour Black => new(0f, 0f, 0f);
    public static Colour Clear => new(0f, 0f, 0f, 0f);
    
    /// <summary>
    /// Size of a Colour in bytes
    /// </summary>
    public const int Size = 16;

    #region Operators

    public static Colour operator +(Colour a, Colour b)
    {
        return new Colour(a.R + b.R, a.G + b.G, a.B + b.B, a.A + b.A);
    }
    
    public static Colour operator -(Colour a, Colour b)
    {
        return new Colour(a.R - b.R, a.G - b.G, a.B - b.B, a.A - b.A);
    }
    
    // TODO: Implement more operators like multiply
    
    public static implicit operator Color4(Colour c)
    {
        return new Color4(c.R, c.G, c.B, c.A);
    }

    #endregion

    #region Utility Methods

    public static Colour Lerp(Colour a, Colour b, float t)
    {
        return new Colour(Mathf.Lerp(a.R, b.R, t), Mathf.Lerp(a.G, b.G, t), Mathf.Lerp(a.B, b.B, t),
            Mathf.Lerp(a.A, b.A, t));
    }
    
    public static Colour ConvertBase(int r, int g, int b, int a = 255)
    {
        return new Colour(r / 255f, g / 255f, b / 255f, a / 255f);
    }

    #endregion
}