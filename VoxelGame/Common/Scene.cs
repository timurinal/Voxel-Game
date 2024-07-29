using System.Diagnostics;
using OpenTK.Mathematics;
using VoxelGame.Rendering;
using Vector3 = VoxelGame.Maths.Vector3;

namespace VoxelGame;

using Vector3 = Maths.Vector3;

internal class Scene : IRenderable
{
    public static Scene Current { get; private set; }
    private static List<Scene> Scenes;

    public string Name;

    private List<Mesh> _meshesToRender = new();
    private List<Mesh> _opaqueRenderables = new();
    private SortedList<(float dst, int index), Mesh> _transparentRenderables = new(new DescComparer<(float dst, int index)>());

    static Scene()
    {
        // Ensure the Scenes list is created
        Scenes = new();
    }

    public static void ChangeScene(int sceneIndex)
    {
        if (sceneIndex < 0 || sceneIndex >= Scenes.Count)
        {
            throw new IndexOutOfRangeException();
        }
        Current = Scenes[sceneIndex];
    }

    public static void ChangeScene(string sceneName)
    {
        foreach (var scene in Scenes)
        {
            if (scene.Name == sceneName)
            {
                Current = scene;
                return;
            }
        }

        // If the scene isn't found, don't do anything
    }

    public static Scene CreateScene(string sceneName)
    {
        var scene = new Scene
        {
            Name = sceneName
        };

        // The engine automatically creates a scene when the game loads
        // so if there are no scenes in the scenes list, this is most likely
        // the scene the engine created so mark it as the currently active scene
        if (Scenes.Count <= 0)
            Current = scene;
        
        Scenes.Add(scene);
        
        return scene;
    }
    
    public static void RemoveScene(int sceneIndex)
    {
        // The first scene is automatically created by the engine and cannot be removed
        if (sceneIndex == 0)
        {
            throw new Exception("Cannot remove main scene!");
        }
        
        if (sceneIndex < 0 || sceneIndex >= Scenes.Count)
        {
            throw new IndexOutOfRangeException();
        }
        Scenes.RemoveAt(sceneIndex);
        
        // Set the current scene to the next available scene
        if (Current == Scenes[sceneIndex])
        {
            Current = Scenes[0];
        }
    }

    internal void AddMesh(Mesh mesh)
    {
        _meshesToRender.Add(mesh);
    }

    public void Render(Camera camera)
    {
        SortMeshes(camera.Position);
        
        // Render opaque geometry
        foreach (var mesh in _opaqueRenderables)
        {
            mesh.Render(camera);
        }

        // Enable blending to render transparent geometry
        GL.Enable(EnableCap.Blend);

        foreach (var mesh in _transparentRenderables.Values)
        {
            mesh.Render(camera);
        }
        
        GL.Disable(EnableCap.Blend);
    }

    public void Render(Matrix4 m_projview)
    {
        foreach (var mesh in _opaqueRenderables)
        {
            mesh.Render(m_projview);
        }
    }

    public void Render(Matrix4 m_proj, Matrix4 m_view)
    {
        foreach (var mesh in _opaqueRenderables)
        {
            mesh.Render(m_proj, m_view);
        }
    }

    public static long _ustotal = 1;
    public static int _samples = 1;
    
    // Function that sorts which meshes are opaque or transparent for correct rendering. Also goes through the transparent meshes and sorts them from back-to-front
    private void SortMeshes(Vector3 cameraPos)
    {
        var timer = new Stopwatch();
        timer.Start();
    
        const float nearTransparentMeshThreshold = 100f;
        const float farTransparentMeshThreshold = 500f;
        const float sqrNearTransparentMeshThreshold = nearTransparentMeshThreshold * nearTransparentMeshThreshold;
        const float sqrFarTransparentMeshThreshold = farTransparentMeshThreshold * farTransparentMeshThreshold;

        List<Mesh> nearTransparentMeshes = new();

        _opaqueRenderables.Clear();
        _transparentRenderables.Clear();
    
        for (int i = 0; i < _meshesToRender.Count; i++)
        {
            var mesh = _meshesToRender[i];

            if (mesh.Material.surfaceMode == Material.SurfaceMode.Opaque) _opaqueRenderables.Add(mesh);
            if (mesh.Material.surfaceMode == Material.SurfaceMode.Transparent)
            {
                float sqrDst = Vector3.SqrDistance(mesh.Transform.Position, cameraPos);

                var key = (sqrDst, i);

                if (sqrDst <= sqrFarTransparentMeshThreshold)
                {
                    _transparentRenderables.Add(key, mesh);

                    if (sqrDst <= sqrNearTransparentMeshThreshold)
                        nearTransparentMeshes.Add(mesh);
                } else
                    _opaqueRenderables.Add(mesh);
            }
        }

        foreach (var mesh in nearTransparentMeshes)
        {
            mesh.SortTriangles(cameraPos);
        }
    
        long elapsedMicroseconds = (timer.ElapsedTicks * 1000000) / Stopwatch.Frequency;
        _ustotal += elapsedMicroseconds;
        _samples++;
    }
}