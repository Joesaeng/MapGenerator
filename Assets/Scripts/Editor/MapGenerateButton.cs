using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapGenerator))]
public class MapGenerateButton : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        MapGenerator generator = (MapGenerator)target;
        if(GUILayout.Button("Generate Map"))
        {
            generator.GenerateMap();
        }
    }
}

[CustomEditor(typeof(CellularAutomataMap))]
public class CellularMapButton : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        CellularAutomataMap generator = (CellularAutomataMap)target;
        if (GUILayout.Button("Generate Map"))
        {
            generator.GenerateMap();
        }
    }
}

[CustomEditor(typeof(BSPCellular))]
public class BSPButton : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        BSPCellular generator = (BSPCellular)target;
        if (GUILayout.Button("Generate Map"))
        {
            generator.GenerateMap();
        }
    }
}
