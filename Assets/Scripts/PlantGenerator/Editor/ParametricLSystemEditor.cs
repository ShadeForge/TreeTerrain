using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ParametricLSystemBehaviour))]
public class ParametricLSystemEditor : Editor
{
    private ParametricLSystemBehaviour parametricLSystemBehaviour;
    private MeshTurtleRenderer meshTurtleRenderer;
    private ParametricLSystem pLSystem;

	void Awake () {
		parametricLSystemBehaviour = target as ParametricLSystemBehaviour;
	}

    private string newRule = "Edit new Rule here...";
    private string newConst = "Edit new Constant here...";
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        meshTurtleRenderer = parametricLSystemBehaviour.meshTurtleRenderer;
        if (meshTurtleRenderer == null)
        {
            meshTurtleRenderer = new MeshTurtleRenderer(parametricLSystemBehaviour.gameObject);
            parametricLSystemBehaviour.meshTurtleRenderer = meshTurtleRenderer;
        }

        pLSystem = parametricLSystemBehaviour.pLSystem;
        if (pLSystem == null)
        {
            pLSystem = new ParametricLSystem();
            parametricLSystemBehaviour.pLSystem = pLSystem;
        }

        EditorGUI.BeginChangeCheck();

        pLSystem.axiom = EditorGUILayout.TextField("Axiom", pLSystem.axiom);
        pLSystem.iterations = EditorGUILayout.IntSlider("Iterations", pLSystem.iterations, 0, 10);

        List<string> keys = pLSystem.rules.Keys.ToList<string>();
        foreach (string key in keys)
        {
            for (int i = 0; i < pLSystem.rules[key].Count; i++)
            {
                ParametricLSystem.Rule rule = pLSystem.rules[key][i];
                EditorGUILayout.LabelField(key + " = ");
                EditorGUILayout.BeginHorizontal();

                string condition = EditorGUILayout.TextField(rule.condition, GUILayout.Width(50));
                string rhs = EditorGUILayout.TextField(rule.rValue);
                pLSystem.EditRule(key, i, rhs, condition);
                if (GUILayout.Button("X"))
                {
                    pLSystem.DeleteRule(key, i);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        newRule = EditorGUILayout.TextField(newRule);
        if (GUILayout.Button("Add Rule"))
        {
            string[] ruleStrings = newRule.Split('=');
            if (ruleStrings.Length == 2)
            {
                if (pLSystem.AddRule(ruleStrings[0], ruleStrings[1]))
                {
                    newRule = "Edit new Rule here ...";
                }
                else
                {
                    Debug.LogError("Inspector Input Error: New Rule: '" + newRule + "' is not valid");
                }

            }
            else
            {
                Debug.LogError("Inspector Input Error: New Rule: '" + newRule + "' is not valid");
            }
        }


        Dictionary<string, double> consts = pLSystem.parser.GetConsts();

        foreach (string constantKey in (new List<string>(consts.Keys)))
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(constantKey + " = ");

            string rhs = EditorGUILayout.TextField(consts[constantKey].ToString());
            pLSystem.parser.AddConst(constantKey, Double.Parse(rhs));

            if (GUILayout.Button("X"))
            {
                pLSystem.parser.RemoveConst(constantKey);
            }

            EditorGUILayout.EndHorizontal();
        }

        newConst = EditorGUILayout.TextField(newConst);
        if (GUILayout.Button("Add Constant"))
        {
            string[] constStrings = newConst.Split('=');
            if (constStrings.Length == 2)
            {
                pLSystem.parser.AddConst(constStrings[0], Double.Parse(constStrings[1]));
                newConst = "Edit new Constant here ...";
            }
            else
            {
                Debug.LogError("Inspector Input Error: New Constant: '" + newConst + "' is not valid");
            }
        }

        if (GUILayout.Button("Rebuild"))
        {
            parametricLSystemBehaviour.RenderMesh();
        }

        if (EditorGUI.EndChangeCheck())
        {
            SceneView.RepaintAll();
        }
    }

    [MenuItem("GameObject/ParametricLSystems/Tree")]
    public static ParametricLSystemBehaviour LoadTree()
    {
        GameObject go = new GameObject("Tree");
        ParametricLSystemBehaviour treeBehaviour = go.AddComponent<ParametricLSystemBehaviour>();

        treeBehaviour.meshTurtleRenderer = new TreeMeshTurtleRenderer(go);

        string leafParameters =
            "L(LeafMinCount,LeafMaxCount,LeafMinLength,LeafMaxLength,LeafMinWidth,LeafMaxWidth,LeafMinXRot,LeafMaxXRot,LeafMinZRot,LeafMaxZRot)";

        treeBehaviour.pLSystem.iterations = 5;
        treeBehaviour.pLSystem.axiom = "D(TrunkWidth,0,MainTrunkLength,XSegments,YSegments,PerlinZoom,PerlinStrength)" +
                                       "S(TrunkWidth,0,TrunkLength,XSegments,YSegments,PerlinZoom,PerlinStrength)";
        treeBehaviour.pLSystem.AddRule("D(x,y,z,w,t,f,s)", "D(x+TrunkWidth,y+TrunkWidth,z,w,t,f,s)");
        treeBehaviour.pLSystem.AddRule("S(x,y,z,w,t,f,s)", "J(x*TrunkGrowthRate,w,t)" +
                                                           "[X(-PI/8+TrunkSplitMaxXRot+rnd(TrunkSplitMinXRot,TrunkSplitMaxXRot))" +
                                                           "Y(-PI/8+TrunkSplitMaxYRot+rnd(TrunkSplitMinYRot,TrunkSplitMaxYRot))" +
                                                           "D(x*TrunkGrowthRate,y*TrunkGrowthRate,z,w,t,f,s)S(x,y,z,w,t,f,s)" + leafParameters + "]" +
                                                           "[X(PI/8+TrunkSplitMinXRot+rnd(TrunkSplitMinXRot,TrunkSplitMaxXRot))" +
                                                           "Y(-PI/8+rnd(TrunkSplitMinYRot,TrunkSplitMaxYRot))" +
                                                           "D(x*TrunkGrowthRate,y*TrunkGrowthRate,z,w,t,f,s)S(x,y,z,w,t,f,s)" + leafParameters + "]");
        treeBehaviour.pLSystem.AddRule("J(x,y,z)", "J(x+TrunkWidth*1.25,y,z)");

        treeBehaviour.pLSystem.parser.AddConst("TrunkGrowthRate", 0.5);
        treeBehaviour.pLSystem.parser.AddConst("TrunkSplitMinXRot", 0);
        treeBehaviour.pLSystem.parser.AddConst("TrunkSplitMaxXRot", 0);
        treeBehaviour.pLSystem.parser.AddConst("TrunkSplitMinYRot", 0);
        treeBehaviour.pLSystem.parser.AddConst("TrunkSplitMaxYRot", 0);
        treeBehaviour.pLSystem.parser.AddConst("TrunkWidth", 0.03);
        treeBehaviour.pLSystem.parser.AddConst("MainTrunkLength", 3);
        treeBehaviour.pLSystem.parser.AddConst("TrunkLength", 1);
        treeBehaviour.pLSystem.parser.AddConst("XSegments", 5);
        treeBehaviour.pLSystem.parser.AddConst("YSegments", 5);
        treeBehaviour.pLSystem.parser.AddConst("PerlinZoom", 0.5);
        treeBehaviour.pLSystem.parser.AddConst("PerlinStrength", 0.05);

        treeBehaviour.pLSystem.parser.AddConst("LeafMinCount", 2);
        treeBehaviour.pLSystem.parser.AddConst("LeafMaxCount", 5);
        treeBehaviour.pLSystem.parser.AddConst("LeafMinLength", 0.2);
        treeBehaviour.pLSystem.parser.AddConst("LeafMaxLength", 0.5);
        treeBehaviour.pLSystem.parser.AddConst("LeafMinWidth", 0.1);
        treeBehaviour.pLSystem.parser.AddConst("LeafMaxWidth", 0.3);
        treeBehaviour.pLSystem.parser.AddConst("LeafMinXRot", -Mathf.PI / 8);
        treeBehaviour.pLSystem.parser.AddConst("LeafMaxXRot", Mathf.PI / 8);
        treeBehaviour.pLSystem.parser.AddConst("LeafMinZRot", -Mathf.PI / 8);
        treeBehaviour.pLSystem.parser.AddConst("LeafMaxZRot", Mathf.PI / 8);

        Texture2D woodTex = Resources.Load<Texture2D>("Plants/Textures/wood");
        Material woodMaterial = new Material(Shader.Find("Standard"));
        woodMaterial.mainTexture = woodTex;

        treeBehaviour.meshTurtleRenderer.AddMaterial(woodMaterial);
        
        Texture2D leafTex = Resources.Load<Texture2D>("Plants/Textures/leaf");
        Material leafMaterial = new Material(Shader.Find("Sprites/Default"));
        leafMaterial.mainTexture = leafTex;

        treeBehaviour.meshTurtleRenderer.AddMaterial(leafMaterial);
        
        treeBehaviour.RenderMesh();

        return treeBehaviour;
    }
}
