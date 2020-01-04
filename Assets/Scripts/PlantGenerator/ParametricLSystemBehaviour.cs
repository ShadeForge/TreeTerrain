using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class ParametricLSystemBehaviour : MonoBehaviour {
    public ParametricLSystem pLSystem = new ParametricLSystem();
    public MeshTurtleRenderer meshTurtleRenderer;

    public void RenderMesh()
    {
        string sequence;
        if(!pLSystem.GenerateSequence(out sequence))
        {
            Debug.LogError("Render error: Could not generate sequence");
            return;
        }
        
        Debug.Log("Parsing Successful: " + sequence);
        meshTurtleRenderer.Process(sequence);
    }
}
