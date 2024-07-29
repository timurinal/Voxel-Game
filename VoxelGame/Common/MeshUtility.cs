using VoxelGame.Maths;
using VoxelGame.Rendering;

namespace VoxelGame;

public static class MeshUtility
{
    /// <summary>
    /// Creates a cube mesh with the given material.
    /// </summary>
    /// <param name="material">The material to assign to the cube mesh.</param>
    /// <returns>The created cube mesh.</returns>
    public static Mesh CreateCube(Material material)
    {
        var mesh = new Mesh(material)
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
            
                // bottom face
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
                new Vector2(1, 0),
                new Vector2(0, 0),
                new Vector2(0, 1),
                new Vector2(1, 1),
                
                // right face
                new Vector2(1, 0),
                new Vector2(0, 0),
                new Vector2(0, 1),
                new Vector2(1, 1),
                
                // left face
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1),
                
                // top face
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1),
                
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
                16, 18, 17,
                16, 19, 18,
            
                // bottom face
                20, 22, 21,
                20, 23, 22
            ]
        };
        
        mesh.RecalculateNormals();

        return mesh;
    }

    public static Mesh CreateSphere(Material material, int subdivisions, float radius)
    {
        subdivisions = Mathf.Max(subdivisions, 1);

        int resolution = 1 << subdivisions;
        Vector3[] vertices = new Vector3[(resolution + 1) * (resolution + 1) * 4 - (resolution * 2 - 1) * 3];
        int[] triangles = new int[(1 << (subdivisions * 2 + 3)) * 3];
        CreateOctahedron(vertices, triangles, resolution);

        Vector3[] normals = new Vector3[vertices.Length];
        Normalize(vertices, normals);
        
        Vector2[] uvs = new Vector2[vertices.Length];
        CreateUV(vertices, uvs);
        
        // Vector4[] tangents = new Vector4[vertices.Length];
        // CreateTangents(vertices, tangents);

        if (radius != 1.0f)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] *= radius;
            }
        }

        Mesh mesh = new Mesh(material)
        {
            Vertices = vertices,
            Triangles = triangles,
            Normals = normals,
            Uvs = uvs,
            // Tangents = tangents,
        };

        return mesh;
    }

    private static void CreateOctahedron(Vector3[] vertices, int[] triangles, int resolution)
    {
        Vector3[] directions =
        {
            Vector3.Left,
            Vector3.Back,
            Vector3.Right,
            Vector3.Forward,
        };
        
        int v = 0, vBottom = 0, t = 0;

        for (int i = 0; i < 4; i++) 
        {
            vertices[v++] = Vector3.Down;
        }

        for (int i = 1; i <= resolution; i++) 
        {
            float progress = (float)i / resolution;
            Vector3 from, to;
            vertices[v++] = to = Vector3.Lerp(Vector3.Down, Vector3.Forward, progress);
            for (int d = 0; d < 4; d++) 
            {
                from = to;
                to = Vector3.Lerp(Vector3.Down, directions[d], progress);
                t = CreateLowerStrip(i, v, vBottom, t, triangles);
                v = CreateVertexLine(from, to, i, v, vertices);
                vBottom += i > 1 ? (i - 1) : 1;
            }
            vBottom = v - 1 - i * 4;
        }
        
        for (int i = resolution - 1; i >= 1; i--) 
        {
            float progress = (float)i / resolution;
            Vector3 from, to;
            vertices[v++] = to = Vector3.Lerp(Vector3.Up, Vector3.Forward, progress);
            for (int d = 0; d < 4; d++) 
            {
                from = to;
                to = Vector3.Lerp(Vector3.Up, directions[d], progress);
                t = CreateUpperStrip(i, v, vBottom, t, triangles);
                v = CreateVertexLine(from, to, i, v, vertices);
                vBottom += i + 1;
            }
            vBottom = v - 1 - i * 4;
        }
        
        for (int i = 0; i < 4; i++) 
        {
            triangles[t++] = vBottom;
            triangles[t++] = ++vBottom;
            triangles[t++] = v;
            vertices[v++] = Vector3.Up;
        }
    }
    
    // private static void CreateTangents (Vector3[] vertices, Vector4[] tangents) {
    //     for (int i = 0; i < vertices.Length; i++) {
    //         Vector3 v = vertices[i];
    //         v.Y = 0f;
    //         v = v.Normalized;
    //         Vector4 tangent;
    //         tangent.x = -v.z;
    //         tangent.y = 0f;
    //         tangent.z = v.x;
    //         tangent.w = -1f;
    //         tangents[i] = tangent;
    //     }
    //     
    //     tangents[vertices.Length - 4] = tangents[0] = new Vector3(-1f, 0, -1f).normalized;
    //     tangents[vertices.Length - 3] = tangents[1] = new Vector3(1f, 0f, -1f).normalized;
    //     tangents[vertices.Length - 2] = tangents[2] = new Vector3(1f, 0f, 1f).normalized;
    //     tangents[vertices.Length - 1] = tangents[3] = new Vector3(-1f, 0f, 1f).normalized;
    //     for (int i = 0; i < 4; i++) {
    //         tangents[vertices.Length - 1 - i].w = tangents[i].w = -1f;
    //     }
    // }

    private static int CreateLowerStrip(int steps, int vTop, int vBottom, int t, int[] triangles)
    {
        for (int i = 1; i < steps; i++) 
        {
            triangles[t++] = vBottom;
            triangles[t++] = vTop;
            triangles[t++] = vTop - 1;

            triangles[t++] = vTop++;
            triangles[t++] = vBottom++;
            triangles[t++] = vBottom;
        }
        triangles[t++] = vTop - 1;
        triangles[t++] = vBottom;
        triangles[t++] = vTop;
        return t;
    }
    
    private static int CreateUpperStrip (int steps, int vTop, int vBottom, int t, int[] triangles) 
    {
        triangles[t++] = vTop - 1;
        triangles[t++] = vBottom;
        triangles[t++] = ++vBottom;
        for (int i = 1; i <= steps; i++) 
        {
            triangles[t++] = vTop - 1;
            triangles[t++] = vBottom;
            triangles[t++] = vTop;

            triangles[t++] = vTop++;
            triangles[t++] = vBottom;
            triangles[t++] = ++vBottom;
        }
        return t;
    }

    private static int CreateVertexLine(Vector3 from, Vector3 to, int steps, int v, Vector3[] vertices)
    {
        for (int i = 1; i <= steps; i++)
        {
            vertices[v++] = Vector3.Lerp(from, to, (float)i / steps);
        }

        return v;
    }

    private static void Normalize(Vector3[] vertices, Vector3[] normals)
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            normals[i] = vertices[i] = vertices[i].Normalized;
        }
    }
    
    private static void CreateUV (Vector3[] vertices, Vector2[] uv) 
    {
        float previousX = 1f;
        for (int i = 0; i < vertices.Length; i++) 
        {
            Vector3 v = vertices[i];
            if (v.X == previousX) 
            {
                uv[i - 1].X = 1f;
            }
            previousX = v.X;
            Vector2 textureCoordinates = Vector2.Zero;
            textureCoordinates.X = Mathf.Atan2(v.X, v.Z) / (-2f * Mathf.PI);
            if (textureCoordinates.X < 0f) 
            {
                textureCoordinates.X += 1f;
            }
            textureCoordinates.Y = Mathf.Asin(v.Y) / Mathf.PI + 0.5f;
            uv[i] = textureCoordinates;
        }
        uv[vertices.Length - 4].X = uv[0].X = 0.125f;
        uv[vertices.Length - 3].X = uv[1].X = 0.375f;
        uv[vertices.Length - 2].X = uv[2].X = 0.625f;
        uv[vertices.Length - 1].X = uv[3].X = 0.875f;
    }
}