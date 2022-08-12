using System.Collections;
using UnityEngine;
using UnityEditor;

namespace BuildingGeneration 
{
    [CustomEditor(typeof(BuildingGenerator))]
    public class BuildingGeneratorEditor : Editor 
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            BuildingGenerator buildingGenerator = (BuildingGenerator)target;

            if(GUILayout.Button("Regenerate"))
            {
                buildingGenerator.Generate();
            }
        }
    }
}
