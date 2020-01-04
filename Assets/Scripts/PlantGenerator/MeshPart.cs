using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MeshPart {
    
    protected List<MeshPart> childs = new List<MeshPart>();
    protected Matrix4x4 transform;
    protected MeshPart parent;

    protected MeshPart(Matrix4x4 transform, MeshPart parent = null)
    {
        this.transform = transform;
        this.parent = parent;
    }

    public abstract void buildMesh(ref List<Vector3> vertices, ref List<Vector2> uvs, ref Dictionary<string, List<int>> triangles);

    public void AddChild(MeshPart meshPart)
    {
        childs.Add(meshPart);
    }
}
