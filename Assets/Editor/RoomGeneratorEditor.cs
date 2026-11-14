using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

//迷路の基本単位となる四角い部屋を生成するためのプログラムのエディター拡張
#if UNITY_EDITOR
[CustomEditor(typeof(RoomGenerator))]
public class RoomGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RoomGenerator gen = (RoomGenerator)target;

        if (GUILayout.Button("Generate Room"))
        {
            gen.GenerateRoom();
        }
    }
}
#endif
