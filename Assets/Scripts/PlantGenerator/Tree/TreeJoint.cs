using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeJoint : MeshPart
{
    public const int THICKNESS_INDEX = 0;
    public const int X_SEGMENTS_INDEX = 1;
    public const int Y_SEGMENTS_INDEX = 2;

    public float thickness;
    public float xSegments;
    public float ySegments;

    public TreeJoint(Matrix4x4 transform, float[] parameters, MeshPart parent = null) : base(transform, parent)
    {
        thickness = parameters[THICKNESS_INDEX];
        xSegments = parameters[X_SEGMENTS_INDEX];
        ySegments = parameters[Y_SEGMENTS_INDEX];
    }

    public override void buildMesh(ref List<Vector3> vertices, ref List<Vector2> uvs, ref Dictionary<string, List<int>> triangles)
    {
        int currentIndex = vertices.Count;

        float widthStepSphere = 360f / (xSegments - 1f);
        float heightStepSphere = 360f / (ySegments - 1f);

        float botWidth = Vector3.Distance(new Vector3(0, 0, 0), new Vector3(widthStepSphere, 0, 0));
        float topWidth = Vector3.Distance(new Vector3(0, heightStepSphere, 0), new Vector3(widthStepSphere, heightStepSphere, 0));
        float leftHeight = Vector3.Distance(new Vector3(0, 0, 0), new Vector3(0, heightStepSphere, 0));
        float rightHeight = Vector3.Distance(new Vector3(widthStepSphere, 0, 0), new Vector3(widthStepSphere, heightStepSphere, 0));

        for (int xSegment = 0; xSegment < xSegments - 1; xSegment++)
        {
            for (int ySegment = 0; ySegment < ySegments - 1; ySegment++)
            {
                float leftX = xSegment * widthStepSphere;
                float rightX = (xSegment + 1) * widthStepSphere;

                float frontZ = ySegment * heightStepSphere;
                float backZ = (ySegment + 1) * heightStepSphere;

                Vector3 botLeft = PointOnSphere(leftX, frontZ, thickness);
                Vector3 topLeft = PointOnSphere(leftX, backZ, thickness);
                Vector3 topRight = PointOnSphere(rightX, backZ, thickness);
                Vector3 botRight = PointOnSphere(rightX, frontZ, thickness);

                vertices.Add(transform.MultiplyPoint(botLeft));
                vertices.Add(transform.MultiplyPoint(topLeft));
                vertices.Add(transform.MultiplyPoint(topRight));
                vertices.Add(transform.MultiplyPoint(botRight));

                uvs.Add(new Vector2(xSegment * botWidth, ySegment * leftHeight));
                uvs.Add(new Vector2(xSegment * topWidth, (ySegment + 1) * leftHeight));
                uvs.Add(new Vector2((xSegment + 1) * topWidth, (ySegment + 1) * rightHeight));
                uvs.Add(new Vector2((xSegment + 1) * botWidth, ySegment * rightHeight));
                
                triangles["treeTrunk"].Add(currentIndex + 2);
                triangles["treeTrunk"].Add(currentIndex + 1);
                triangles["treeTrunk"].Add(currentIndex + 0);

                triangles["treeTrunk"].Add(currentIndex + 0);
                triangles["treeTrunk"].Add(currentIndex + 3);
                triangles["treeTrunk"].Add(currentIndex + 2);

                currentIndex += 4;
            }
        }

        foreach (MeshPart child in childs)
        {
            child.buildMesh(ref vertices, ref uvs, ref triangles);
        }
    }

    public static Vector3 PointOnSphere(float lon, float lat, float radius)
    {
        lon = lon * Mathf.Deg2Rad + Mathf.PI / 2f;
        lat = lat * Mathf.Deg2Rad;

        float x = radius * Mathf.Cos(lon) * Mathf.Sin(lat);
        float y = radius * Mathf.Cos(lat);
        float z = radius * Mathf.Sin(lon) * Mathf.Sin(lat);

        Vector3 point = new Vector3(x, -y, z);

        return point;
    }
}
