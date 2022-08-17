using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TextureClip))]
public class TextureClipEditor : Editor
{
    private TextureClip clipper;
    void OnEnable()
    {
        clipper = target as TextureClip;
    }

    public override void OnInspectorGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("裁剪参数, XY对应PS被裁剪框选区中的XY, 单位为像素");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        {
            GUILayout.Label("贴图文件夹路径", GUILayout.Width(90f)); 
            clipper.clipFolder = GUILayout.TextField(clipper.clipFolder);
            if (GUILayout.Button("选择", GUILayout.Width(50f)))
            {
                clipper.clipFolder = EditorUtility.OpenFolderPanel("选择贴图所在文件架", Application.dataPath, "");
            }
        }
        GUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal(); 
        EditorGUILayout.LabelField("宽", GUILayout.MaxWidth(20)); 
        clipper.clipWidth = EditorGUILayout.IntField(clipper.clipWidth);
        EditorGUILayout.LabelField("高", GUILayout.MaxWidth(20));
        clipper.clipHeight = EditorGUILayout.IntField(clipper.clipHeight);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal(); 
        EditorGUILayout.LabelField("X", GUILayout.MaxWidth(20)); 
        clipper.leftTopX = EditorGUILayout.IntField(clipper.leftTopX);
        EditorGUILayout.LabelField("Y", GUILayout.MaxWidth(20));
        clipper.leftTopY = EditorGUILayout.IntField(clipper.leftTopY);
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("裁剪贴图", GUILayout.Height(28)))
        {
            clipper.ApplyClipInFolder();
        }
    }
}
