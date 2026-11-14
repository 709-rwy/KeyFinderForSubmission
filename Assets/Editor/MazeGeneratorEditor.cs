using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

//迷路を生成するためのプログラムのエディター拡張
#if UNITY_EDITOR
[CustomEditor(typeof(MazeGenerator))]
public class MazeGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MazeGenerator gen = (MazeGenerator)target;

        if (GUILayout.Button("Generate Maze"))
        {
            gen.GenerateMaze();
        }
    }
}
#endif