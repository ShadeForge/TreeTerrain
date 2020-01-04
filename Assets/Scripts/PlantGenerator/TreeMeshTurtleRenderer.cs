using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeMeshTurtleRenderer : MeshTurtleRenderer {

    public const char DRAW_TRUNK = 'D';
    public const char DRAW_LEAF = 'L';
    public const char DRAW_JOINT = 'J';

    public TreeMeshTurtleRenderer(GameObject parent) : base(parent)
    {
    }

    public override void ProcessOperation(string operations, ref int i)
    {
        float[] parameters;
        char operation = operations[i];
        // TODO: ContextSensitiv
        switch (operation)
        {
            case DRAW_TRUNK:
                parameters = ProcessParameters(operations, ref i);
                this.AddMeshPart(new TreeTrunk(currentTransform, parameters, currentMeshPart));
                currentTransform *= Matrix4x4.Translate(Vector3.up * parameters[TreeTrunk.LENGTH_INDEX]);
                break;
            case DRAW_LEAF:
                parameters = ProcessParameters(operations, ref i);
                this.AddMeshPart(new TreeLeaf(currentTransform, parameters, currentMeshPart));
                break;
            case DRAW_JOINT:
                parameters = ProcessParameters(operations, ref i);
                this.AddMeshPart(new TreeJoint(currentTransform, parameters, currentMeshPart));
                break;
            default:
                base.ProcessOperation(operations, ref i);
                break;
        }
    }
}
