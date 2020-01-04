using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MeshTurtleRenderer {

    public const char ROTATE_X = 'X';
    public const char ROTATE_Y = 'Y';
    public const char ROTATE_Z = 'Z';
    public const char PUSH_STATE = '[';
    public const char POP_STATE = ']';

    protected GameObject parent;

    protected Mesh mesh;
    protected MeshFilter meshFilter;
    protected MeshRenderer meshRenderer;

    protected List<Vector3> vertices = new List<Vector3>();
    protected List<Vector2> uvs = new List<Vector2>();
    protected Dictionary<string, List<int>> triangles = new Dictionary<string, List<int>>();
    protected Stack<Matrix4x4> transformStack = new Stack<Matrix4x4>();
    protected Stack<MeshPart> meshPartStack = new Stack<MeshPart>();

    protected Matrix4x4 currentTransform;
    protected MeshPart currentMeshPart;
    protected MeshPart rootMeshPart;

    protected List<Material> materials = new List<Material>();

    public MeshTurtleRenderer(GameObject parent)
    {
        this.parent = parent;
    }

    protected virtual void InitializeProcessing()
    {
        meshFilter = parent.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = parent.AddComponent<MeshFilter>();
        }

        meshRenderer = parent.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = parent.AddComponent<MeshRenderer>();
        }

        mesh = meshFilter.sharedMesh;
        if (mesh == null)
        {
            mesh = new Mesh();
        }

        vertices = new List<Vector3>();
        uvs = new List<Vector2>();
        triangles = new Dictionary<string, List<int>>();
        transformStack = new Stack<Matrix4x4>();
        meshPartStack = new Stack<MeshPart>();
        currentTransform = Matrix4x4.identity;
        currentMeshPart = null;
        rootMeshPart = null;
    }

    public void Process(string operations)
    {
        InitializeProcessing();

        for (int i = 0; i < operations.Length; i++)
        {
            ProcessOperation(operations, ref i);
        }

        FinishProcessing();
    }

    protected virtual void FinishProcessing()
    {
        if (rootMeshPart != null)
        {
            mesh.Clear();
            rootMeshPart.buildMesh(ref vertices, ref uvs, ref triangles);

            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = triangles.Count;

            for (int i = 0; i < triangles.Count; i++)
            {
                mesh.SetTriangles(triangles[triangles.Keys.ToArray()[i]], i);
            }

            mesh.RecalculateNormals();
            mesh.name = "Mesh";
            meshFilter.sharedMesh = mesh;

            meshRenderer.materials = materials.ToArray();
        }
        else
        {
            Debug.LogWarning("Mesh Build Warning: No mesh was built");
        }
    }

    public virtual void ProcessOperation(string operations, ref int i)
    {
        float[] parameters;
        switch (operations[i])
        {
            case ROTATE_X:
                parameters = ProcessParameters(operations, ref i);
                currentTransform *= Matrix4x4.Rotate(Quaternion.AngleAxis(parameters[0] * Mathf.Rad2Deg, new Vector3(1, 0, 0)));
                break;
            case ROTATE_Y:
                parameters = ProcessParameters(operations, ref i);
                currentTransform *= Matrix4x4.Rotate(Quaternion.AngleAxis(parameters[0] * Mathf.Rad2Deg, new Vector3(0, 1, 0)));
                break;
            case ROTATE_Z:
                parameters = ProcessParameters(operations, ref i);
                currentTransform *= Matrix4x4.Rotate(Quaternion.AngleAxis(parameters[0] * Mathf.Rad2Deg, new Vector3(0, 0, 1)));
                break;
            case PUSH_STATE:
                transformStack.Push(currentTransform);
                meshPartStack.Push(currentMeshPart);
                break;
            case POP_STATE:
                if (transformStack.Count != 0)
                {
                    currentTransform = transformStack.Pop();
                }
                else
                {
                    Debug.LogWarning("Processing Error: POP used on empty transform stack");
                }

                if (meshPartStack.Count != 0)
                {
                    currentMeshPart = meshPartStack.Pop();
                }
                else
                {
                    Debug.LogWarning("Processing Error: POP used on empty meshpart stack");
                }
                break;
        }
    }

    protected float[] ProcessParameters(string operations, ref int i)
    {
        operations = operations.Substring(i + 2);
        operations = operations.Remove(operations.IndexOf(')'));

        string[] parameterStrs = operations.Split(',');

        float[] parameters = new float[parameterStrs.Length];

        for (int j = 0; j < parameterStrs.Length; j++)
        {
            float.TryParse(parameterStrs[j], out parameters[j]);
        }

        i += operations.Length + 2;

        return parameters;
    }

    public void AddMeshPart(MeshPart meshPart)
    {
        if (rootMeshPart == null)
        {
            rootMeshPart = meshPart;
            currentMeshPart = meshPart;
        }
        else
        {
            currentMeshPart.AddChild(meshPart);
            currentMeshPart = meshPart;
        }
    }

    public void AddMaterial(Material material)
    {
        materials.Add(material);
    }
}
