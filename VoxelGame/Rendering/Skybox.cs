using OpenTK.Mathematics;
using StbImageSharp;
using Vector3 = VoxelGame.Maths.Vector3;

namespace VoxelGame.Rendering;

internal class Skybox : IRenderable
{
    private Mesh _skyPlane;
    private Mesh _voidPlane;

    private Shader _shader;
    
    internal Skybox(Shader skyShader, Shader voidShader)
    {
        _skyPlane = new Mesh(skyShader)
        {
            Vertices = 
            [
                new(-0.5f, 5f, -0.5f),
                new(-0.5f, 5f,  0.5f),
                new( 0.5f, 5f, -0.5f),
                new( 0.5f, 5f,  0.5f),
            ],
            Uvs = 
            [
                new(0, 0),
                new(0, 1),
                new(1, 0),
                new(1, 1),
            ],
            Colours = 
            [
                Colour.ConvertBase(94, 155, 255),
                Colour.ConvertBase(94, 155, 255),
                Colour.ConvertBase(94, 155, 255),
                Colour.ConvertBase(94, 155, 255),
            ],
            Triangles = 
            [
                0, 2, 1,
                2, 3, 1,
            ],
            Transform =
            {
                Scale = new Vector3(100, 1, 100)
            }
        };
        
        _voidPlane = new Mesh(voidShader)
        {
            Vertices = 
            [
                new(-0.5f, -5f, -0.5f),
                new(-0.5f, -5f,  0.5f),
                new( 0.5f, -5f, -0.5f),
                new( 0.5f, -5f,  0.5f),
            ],
            Uvs = 
            [
                new(0, 0),
                new(0, 1),
                new(1, 0),
                new(1, 1),
            ],
            Colours = 
            [
                new(0.1f, 0.1f, 0.1f),
                new(0.1f, 0.1f, 0.1f),
                new(0.1f, 0.1f, 0.1f),
                new(0.1f, 0.1f, 0.1f),
            ],
            Triangles = 
            [
                0, 1, 2,
                2, 1, 3,
            ],
            Transform =
            {
                Scale = new Vector3(200, 1, 200)
            }
        };
    }

    public (int vertexCount, int triangleCount) Render(Player player)
    {
        _skyPlane.Transform.Position = player.Position;
        _voidPlane.Transform.Position = player.Position;
        
        _skyPlane.Render(player);
        _voidPlane.Render(player);

        return (0, 0);
    }
}