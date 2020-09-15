using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

[CustomEditor(typeof(Example))]
public class ExampleGUI : Editor
{
    Example exmpl;
    private void OnEnable()
    {
        exmpl = (Example)target;
    }

    public override void OnInspectorGUI()
    {
        exmpl.X = EditorGUILayout.Slider(exmpl.X, -1, 1);
        exmpl.Y = EditorGUILayout.Slider(exmpl.Y, -1, 1);
        exmpl.Z = EditorGUILayout.Slider(exmpl.Z, -1, 1);
        

        SerializedProperty textX = serializedObject.FindProperty("m_TextX");
        SerializedProperty textY = serializedObject.FindProperty("m_TextY");
        SerializedProperty textZ = serializedObject.FindProperty("m_TextZ");
        EditorGUILayout.PropertyField(textX, new GUIContent("TextX"), true);
        EditorGUILayout.PropertyField(textY, new GUIContent("TextY"), true);
        EditorGUILayout.PropertyField(textZ, new GUIContent("TextZ"), true);
        serializedObject.ApplyModifiedProperties();

        exmpl.OnQuaternionChanged();
    }
}
