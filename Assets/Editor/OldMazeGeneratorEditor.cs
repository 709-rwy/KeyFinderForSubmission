using UnityEditor;
using UnityEngine;

//現在は使用していない古い迷路を生成するためのエディター拡張
#if UNITY_EDITOR
[CustomEditor(typeof(OldMazeGenerator))]
public class OldMazeGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        OldMazeGenerator gen = (OldMazeGenerator)target;

        if (GUILayout.Button("Generate Maze"))
        {
            gen.GenerateMaze();
        }
    }
}
#endif
