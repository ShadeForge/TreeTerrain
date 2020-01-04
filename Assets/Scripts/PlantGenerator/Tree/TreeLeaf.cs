using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeLeaf : MeshPart
{
    public const int LEAF_MIN_COUNT_INDEX = 0;
    public const int LEAF_MAX_COUNT_INDEX = 1;
    public const int LEAF_MIN_LENGTH_INDEX = 2;
    public const int LEAF_MAX_LENGTH_INDEX = 3;
    public const int LEAF_MIN_WIDTH_INDEX = 4;
    public const int LEAF_MAX_WIDTH_INDEX = 5;
    public const int LEAF_MIN_XROTATION_INDEX = 6;
    public const int LEAF_MAX_XROTATION_INDEX = 7;
    public const int LEAF_MIN_ZROTATION_INDEX = 8;
    public const int LEAF_MAX_ZROTATION_INDEX = 9;

    private float leafMinCount;
    private float leafMaxCount;
    private float leafMaxLength;
    private float leafMinLength;
    private float leafMinWidth;
    private float leafMaxWidth;
    private float leafMinXRotation;
    private float leafMaxXRotation;
    private float leafMinZRotation;
    private float leafMaxZRotation;
    public TreeLeaf(Matrix4x4 transform, float[] parameters, MeshPart parent = null) : base(transform, parent)
    {
        leafMinCount = parameters[LEAF_MIN_COUNT_INDEX];
        leafMaxCount = parameters[LEAF_MAX_COUNT_INDEX];
        leafMaxLength = parameters[LEAF_MIN_LENGTH_INDEX];
        leafMinLength = parameters[LEAF_MAX_LENGTH_INDEX];
        leafMinWidth = parameters[LEAF_MIN_WIDTH_INDEX];
        leafMaxWidth = parameters[LEAF_MAX_WIDTH_INDEX];
        leafMinXRotation = parameters[LEAF_MIN_XROTATION_INDEX];
        leafMaxXRotation = parameters[LEAF_MAX_XROTATION_INDEX];
        leafMinZRotation = parameters[LEAF_MIN_ZROTATION_INDEX];
        leafMaxZRotation = parameters[LEAF_MAX_ZROTATION_INDEX];
    }

    public override void buildMesh(ref List<Vector3> vertices, ref List<Vector2> uvs, ref Dictionary<string, List<int>> triangles)
    {
        System.Random rand = new System.Random();

        int leafCount = rand.Next((int)leafMinCount, (int)leafMaxCount);

        for (int i = 0; i < leafCount; i++)
        {
            float leafLength = Random.Range(leafMinLength, leafMaxLength);
            float leafWidth = Random.Range(leafMinWidth, leafMaxWidth);
            float leafXRotation = Random.Range(leafMinXRotation, leafMaxXRotation);
            float leafZRotation = Random.Range(leafMinZRotation, leafMaxZRotation);

            Matrix4x4 transform = this.transform * Matrix4x4.Rotate(Quaternion.AngleAxis(leafXRotation * Mathf.Rad2Deg, new Vector3(1, 0, 0)));
            transform *= Matrix4x4.Rotate(Quaternion.AngleAxis(leafZRotation * Mathf.Rad2Deg, new Vector3(0, 0, 1)));

            Vector3 botLeft = transform.MultiplyPoint(new Vector3(0, 0, 0));
            Vector3 topLeft = transform.MultiplyPoint(new Vector3(0, 0, leafLength));
            Vector3 topRight = transform.MultiplyPoint(new Vector3(leafWidth, 0, leafLength));
            Vector3 botRight = transform.MultiplyPoint(new Vector3(leafWidth, 0, 0));

            int currentIndex = vertices.Count;

            vertices.Add(botLeft);
            vertices.Add(topLeft);
            vertices.Add(topRight);
            vertices.Add(botRight);

            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(0, 1));
            uvs.Add(new Vector2(1, 1));
            uvs.Add(new Vector2(1, 0));

            if (!triangles.ContainsKey("treeLeaf"))
            {
                triangles.Add("treeLeaf", new List<int>());
            }

            triangles["treeLeaf"].Add(currentIndex + 2);
            triangles["treeLeaf"].Add(currentIndex + 1);
            triangles["treeLeaf"].Add(currentIndex + 0);

            triangles["treeLeaf"].Add(currentIndex + 0);
            triangles["treeLeaf"].Add(currentIndex + 3);
            triangles["treeLeaf"].Add(currentIndex + 2);
        }

        foreach (MeshPart child in childs)
        {
            child.buildMesh(ref vertices, ref uvs, ref triangles);
        }
    }
}
