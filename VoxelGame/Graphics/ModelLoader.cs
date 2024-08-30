using VoxelGame.Maths;

namespace VoxelGame.Graphics;

public static class ModelLoader
{
    public static void LoadModel(string pathToObj, string modelName)
    {
        string path = Path.Combine(pathToObj, modelName);
        if (!File.Exists(path)) throw new IOException($"File does not exist at path: {path}");
        
        List<Vector3> vertices = [];
        List<Vector3> normals = [];
        List<Vector2> uvs = [];
        
        StreamReader streamReader = new StreamReader(path);

        while (!streamReader.EndOfStream)
        {
            string line = streamReader.ReadLine() ?? string.Empty;
            if (line.StartsWith("#")) continue;
            
            if (line.StartsWith("mtllib ")) LoadMaterial(pathToObj, line);

            if (line.StartsWith("o "))
            {
                // Remove the 'o ' from the start of the object name
                string ln = line.Remove(0, 2);
                Console.WriteLine($"Model Name: {ln}");
            }

            if (line.StartsWith("v "))
            {
                string ln = line.Remove(0, 2);
                Vector3 vertex = ParseVector3(ln);
                vertices.Add(vertex);
            }
            if (line.StartsWith("vn "))
            {
                string ln = line.Remove(0, 3);
                Vector3 normal = ParseVector3(ln);
                normals.Add(normal);
            }
            if (line.StartsWith("vt "))
            {
                string ln = line.Remove(0, 3);
                Vector2 uv = ParseVector2(ln);
                uvs.Add(uv);
            }
        }

        int vertexCount = vertices.Count;
        
        Vertex[] vertexGroup = new Vertex[vertexCount];

        for (int i = 0; i < vertexCount; i++)
        {
            Vertex v = new Vertex();
            v.vertex = vertices[i];
            v.normal = normals[i];
            v.uv = uvs[i];
            vertexGroup[i] = v;
        }
    }

    private static string LoadMaterial(string modelRoot, string path)
    {
        // Remove the 'mtllib ' from the start of the path
        path = path.Remove(0, 7);
        path = modelRoot + "/" + path;
        
        // TODO
        string material = File.ReadAllText(path);
        
        return "";
    }

    private static Vector3 ParseVector3(string line)
    {
        string[] components = line.Split(' ');
        float x = float.Parse(components[0]);
        float y = float.Parse(components[1]);
        float z = float.Parse(components[2]);
        return new Vector3(x, y, z);
    }
    private static Vector2 ParseVector2(string line)
    {
        string[] components = line.Split(' ');
        float x = float.Parse(components[0]);
        float y = float.Parse(components[1]);
        return new Vector2(x, y);
    }

    private struct Vertex
    {
        public Vector3 vertex;
        public Vector3 normal;
        public Vector2 uv;
    }
}