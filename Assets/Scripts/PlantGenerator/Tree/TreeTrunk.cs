using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeTrunk : MeshPart
{
    public const int BOTTOM_THICKNESS_INDEX = 0;
    public const int TOP_THICKNESS_INDEX = 1;
    public const int LENGTH_INDEX = 2;
    public const int X_SEGMENT_COUNT_INDEX = 3;
    public const int Y_SEGMENT_COUNT_INDEX = 4;
    public const int PERLIN_NOISE_ZOOM_INDEX = 5;
    public const int PERLIN_NOISE_STRENGTH_INDEX = 6;

    public float botThickness;
    public float topThickness;
    public float length;
    public float xSegments;
    public float ySegments;
    public float pNZoom;
    public float pNStrength;

    public TreeTrunk(Matrix4x4 transform, float[] parameters, MeshPart parent = null) : base(transform, parent)
    {
        this.botThickness = parameters[BOTTOM_THICKNESS_INDEX];
        this.topThickness = parameters[TOP_THICKNESS_INDEX];
        this.length = parameters[LENGTH_INDEX];
        this.xSegments = parameters[X_SEGMENT_COUNT_INDEX];
        this.ySegments = parameters[Y_SEGMENT_COUNT_INDEX];
        this.pNZoom = parameters[PERLIN_NOISE_ZOOM_INDEX];
        this.pNStrength = parameters[PERLIN_NOISE_STRENGTH_INDEX];
    }

    public override void buildMesh(ref List<Vector3> vertices, ref List<Vector2> uvs, ref Dictionary<string, List<int>> triangles)
    {
        int currentIndex = vertices.Count;
        float xStep = (Mathf.PI * 2) / (xSegments);
        float yStep = length / (ySegments);

        if (!triangles.ContainsKey("treeTrunk"))
        {
            triangles.Add("treeTrunk", new List<int>());
        }

        for (int ySegment = 0; ySegment < ySegments + 1; ySegment++)
        {
            for (int xSegment = 0; xSegment < xSegments + 1; xSegment++)
            {
                float factor = ySegment * yStep / length;
                float radius = botThickness + (topThickness - botThickness) * factor;
                if(radius != 0)
                    radius += Mathf.PerlinNoise(xSegment * pNZoom, ySegment * pNZoom) * pNStrength;
                float x = Mathf.Sin(xStep * xSegment) * radius;
                float z = Mathf.Cos(xStep * xSegment) * radius;
                float y = ySegment * yStep;

                vertices.Add(transform.MultiplyPoint(new Vector3(x, y, z)));
                uvs.Add(new Vector2(xSegment * xStep, ySegment * yStep));
            }
        }

        for (int ySegment = 0; ySegment <= ySegments; ySegment++)
        {
            for (int xSegment = 0; xSegment < xSegments; xSegment++)
            {
                triangles["treeTrunk"].Add(currentIndex + ((ySegment + 1) * (int)xSegments + xSegment + 1));
                triangles["treeTrunk"].Add(currentIndex + ((ySegment + 1) * (int)xSegments + xSegment));
                triangles["treeTrunk"].Add(currentIndex + (ySegment * (int)xSegments + xSegment));

                triangles["treeTrunk"].Add(currentIndex + (ySegment * (int)xSegments + xSegment));
                triangles["treeTrunk"].Add(currentIndex + (ySegment * (int)xSegments + xSegment + 1));
                triangles["treeTrunk"].Add(currentIndex + ((ySegment + 1) * (int)xSegments + xSegment + 1));
            }
        }

        foreach (MeshPart child in childs)
        {
            child.buildMesh(ref vertices, ref uvs, ref triangles);
        }
    }
}
