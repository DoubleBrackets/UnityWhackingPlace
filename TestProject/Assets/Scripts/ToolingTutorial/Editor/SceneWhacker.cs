#region

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

#endregion

public class SceneWhacker : EditorWindow
{
    [MenuItem("Tools/SceneMultiLoad")]
    private static void OpenSceneLoader() => GetWindow<SceneWhacker>("Scene Whacker");

    private SceneAsset scene1;
    private SceneAsset scene2;

    private void OnGUI()
    {
        scene1 = (SceneAsset)EditorGUILayout.ObjectField("Scene 1", (Object)scene1, typeof(SceneAsset), true);
        scene2 = (SceneAsset)EditorGUILayout.ObjectField("Scene 2", (Object)scene2, typeof(SceneAsset), true);

        if (GUILayout.Button("Load scene group"))
        {
            EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(scene1), OpenSceneMode.Additive);
            EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(scene2), OpenSceneMode.Additive);
        }
    }
}