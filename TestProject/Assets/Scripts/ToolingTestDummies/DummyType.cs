#region

using UnityEditor;
using UnityEngine;

#endregion

public class DummyType : MonoBehaviour
{
}

[CustomPropertyDrawer(typeof(DummyType))]
public class DummyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUILayout.IntField("Test", 0);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return base.GetPropertyHeight(property, label);
    }
}