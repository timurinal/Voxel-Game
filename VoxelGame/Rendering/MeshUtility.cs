using VoxelGame.Maths;

namespace VoxelGame.Rendering;

public static class MeshUtility
{
    public static Mesh GenerateCube(Shader shader, Vector3 position, Vector3 rotation, Vector3 scale)
    {
        var mesh = new Mesh(shader, position, rotation, scale)
        {
            Vertices =
            [
                // front face
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f),
            
                // back face
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f),
            
                // right face
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
            
                // left face
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f),
            
                // top face
                new Vector3(-0.5f, 0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f),
            
                // top face
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f),
            ],
            Uvs =
            [
                // front face
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1),
                
                // back face
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1),
                
                // right face
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1),
                
                // left face
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1),
                
                // top face
                new Vector2(0, 1),
                new Vector2(1, 1),
                new Vector2(1, 0),
                new Vector2(0, 0),
                
                // bottom face
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1),
            ],
            Colours =
            [
                Colour.Red, 
                Colour.Green, 
                Colour.Blue, 
                Colour.Yellow, 
            
                Colour.Red, 
                Colour.Green, 
                Colour.Blue, 
                Colour.Yellow, 
            
                Colour.Red, 
                Colour.Green, 
                Colour.Blue, 
                Colour.Yellow, 
            
                Colour.Red, 
                Colour.Green, 
                Colour.Blue, 
                Colour.Yellow, 
            
                Colour.Red, 
                Colour.Green, 
                Colour.Blue, 
                Colour.Yellow, 
            
                Colour.Red, 
                Colour.Green, 
                Colour.Blue, 
                Colour.Yellow, 
            ],
            Triangles =
            [
                // front face
                0, 2, 1,
                0, 3, 2,
            
                // back face
                4, 5, 6,
                4, 6, 7,
            
                // right face
                8, 9, 10,
                8, 10, 11,
            
                // left face
                12, 14, 13,
                12, 15, 14,
            
                // top face
                17, 16, 18,
                18, 16, 19,
            
                // bottom face
                20, 22, 21,
                20, 23, 22
            ]
        };

        return mesh;
    }
    public static Mesh GenerateCube(Shader shader)
    {
        return GenerateCube(shader, Vector3.Zero, Vector3.Zero, Vector3.One);
    }
}