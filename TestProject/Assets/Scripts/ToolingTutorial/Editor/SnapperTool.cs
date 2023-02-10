#region

using UnityEditor;
using UnityEngine;

#endregion

internal class SnapperToolSettings
{
    public Vector3 cartesianSnapInterval = new(1, 1, 1);
    public float cartesianGridDisplayLength = 6f;

    public float polarSnapThetaInterval = 45f;
    public float polarSnapRadiusInterval = 1f;
    public float polarGridDisplayRadius = 5f;

    public SnapperTool.SnapMode currentMode;
}

public class SnapperTool : EditorWindow
{
    public enum SnapMode
    {
        Cartesian,
        Polar
    }

    private SnapperToolSettings settingsInstance;


    private const string SETTINGS_PREF_KEY = "SnapperToolsSettings";

    [MenuItem("Tools/Snapper")]
    public static void Open() => GetWindow<SnapperTool>(title: "SNAPPIN TIME");

    private void OnEnable()
    {
        // Load snap values from prefs
        LoadFromPrefs();
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        // Save snap values to prefs
        SaveToPrefs();
        SceneView.duringSceneGui -= OnSceneGUI;
    }


    private void LoadFromPrefs()
    {
        string prefsJson = EditorPrefs.GetString(SETTINGS_PREF_KEY, string.Empty);
        settingsInstance = (prefsJson == string.Empty) ? new SnapperToolSettings() : JsonUtility.FromJson<SnapperToolSettings>(prefsJson);
    }

    private void SaveToPrefs()
    {
        EditorPrefs.SetString(SETTINGS_PREF_KEY, JsonUtility.ToJson(settingsInstance));
    }

    private void ClearPrefs()
    {
        EditorPrefs.DeleteKey(SETTINGS_PREF_KEY);
    }

    private void OnSelectionChange()
    {
        Repaint();
    }

    private void OnGUI()
    {
        using (new GUILayout.VerticalScope())
        {
            GUILayout.Label("Snap Mode", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
            settingsInstance.currentMode = (SnapMode)EditorGUILayout.EnumPopup(settingsInstance.currentMode, new GUIStyle(EditorStyles.popup) { alignment = TextAnchor.MiddleCenter });
        }

        // Cartesian Settings
        if (settingsInstance.currentMode == SnapMode.Cartesian)
        {
            using (new GUILayout.VerticalScope())
            {
                GUILayout.Label("Cartesian Snapping");
                settingsInstance.cartesianSnapInterval = EditorGUILayout.Vector3Field("Snap Intervals", settingsInstance.cartesianSnapInterval);
                settingsInstance.cartesianGridDisplayLength = EditorGUILayout.FloatField("Grid Length", settingsInstance.cartesianGridDisplayLength);
            }
        }

        // Polar settings
        if (settingsInstance.currentMode == SnapMode.Polar)
        {
            using (new GUILayout.VerticalScope())
            {
                GUILayout.Label("Polar Snapping");
                settingsInstance.polarSnapThetaInterval = EditorGUILayout.FloatField("Angle Interval (Degrees)", settingsInstance.polarSnapThetaInterval);
                settingsInstance.polarSnapRadiusInterval = EditorGUILayout.FloatField("Radius Interval", settingsInstance.polarSnapRadiusInterval);
                settingsInstance.polarGridDisplayRadius = EditorGUILayout.FloatField("Grid Radius", settingsInstance.polarGridDisplayRadius);
            }
        }

        using (new EditorGUI.DisabledScope(Selection.gameObjects.Length == 0))
        {
            SnapButton();
        }

        SceneView.RepaintAll();
    }

    private void SnapButton()
    {
        // Snap button
        var selection = Selection.gameObjects;
        if (GUILayout.Button("Snap Selection"))
        {
            foreach (var go in selection)
            {
                Undo.RecordObject(go.transform, "Snap Transform");
                switch (settingsInstance.currentMode)
                {
                    case SnapMode.Cartesian:
                        go.transform.position = CartesianSnap(go.transform.position, settingsInstance.cartesianSnapInterval);
                        break;
                    case SnapMode.Polar:
                        go.transform.position = PolarSnap(go.transform.position, settingsInstance.polarSnapThetaInterval, settingsInstance.polarSnapRadiusInterval);
                        break;
                }
            }
        }
    }

    private void OnSceneGUI(SceneView view)
    {
        if (Event.current.type != EventType.Repaint)
            return;

        var selection = Selection.gameObjects;
        foreach (var go in selection)
        {
            Handles.color = new Color(0.68f, 0.72f, 0.79f);
            Vector3 cPos = go.transform.position;
            Vector3 snappedPoint = Vector3.zero;
            if (settingsInstance.currentMode == SnapMode.Cartesian)
            {
                snappedPoint = CartesianSnap(go.transform.position, settingsInstance.cartesianSnapInterval);
                DrawCartesianGridAroundPoint(snappedPoint, settingsInstance.cartesianSnapInterval, settingsInstance.cartesianGridDisplayLength);
            }

            if (settingsInstance.currentMode == SnapMode.Polar)
            {
                Vector3 center = new Vector3(0, snappedPoint.y, 0);
                snappedPoint = PolarSnap(cPos,
                    settingsInstance.polarSnapThetaInterval,
                    settingsInstance.polarSnapRadiusInterval);

                DrawPolarGridAroundPoint(center,
                    settingsInstance.polarSnapThetaInterval,
                    settingsInstance.polarSnapRadiusInterval,
                    settingsInstance.polarGridDisplayRadius);
            }

            Handles.color = new Color(0.26f, 0.89f, 0.14f);
            Handles.DrawLine(cPos, snappedPoint);
            Handles.DrawSolidDisc(snappedPoint, Vector3.up, 0.25f);
        }
    }

    private void DrawCartesianGridAroundPoint(Vector3 point, Vector3 intervals, float radius)
    {
        int zLineCount = (int)(radius / intervals.z);
        int xLineCount = (int)(radius / intervals.x);
        for (int z = -zLineCount; z <= zLineCount; z++)
        {
            Vector3 line = point + new Vector3(0, 0, z * intervals.z);
            Vector3 xOffset = new Vector3(xLineCount * intervals.x, 0, 0);
            Handles.DrawLine(line + xOffset, line - xOffset);
        }

        for (int x = -xLineCount; x <= xLineCount; x++)
        {
            Vector3 line = point + new Vector3(x * intervals.x, 0, 0);
            Vector3 zOffset = new Vector3(0, 0, zLineCount * intervals.z);
            Handles.DrawLine(line + zOffset, line - zOffset);
        }
    }

    private void DrawPolarGridAroundPoint(Vector3 point, float angleInterval, float radiusInterval, float radius)
    {
        // Draw circles
        for (float i = radiusInterval; i < radius; i += radiusInterval)
        {
            Handles.DrawWireDisc(point, Vector3.up, i);
        }

        Handles.color = new Color(0.79f, 0.3f, 0.32f);
        // Draw angle lines
        for (float i = 0; i < 360f; i += angleInterval)
        {
            float angleRad = Mathf.Deg2Rad * i;
            Handles.DrawLine(point, point + new Vector3(Mathf.Cos(angleRad), 0, Mathf.Sin(angleRad)) * radius);
        }
    }

    private static Vector3 PolarSnap(Vector3 pos, float thetaInterval, float radiusInterval)
    {
        float intervalRads = thetaInterval * Mathf.Deg2Rad;
        float cAngle = Mathf.Atan2(pos.z, pos.x);
        if (cAngle < 0)
            cAngle = 2 * Mathf.PI + cAngle;
        float newAngle = SnapFloat(cAngle, intervalRads);
        float newDist = SnapFloat(pos.magnitude, radiusInterval);
        return new Vector3(Mathf.Cos(newAngle) * newDist, pos.y, Mathf.Sin(newAngle) * newDist);
    }

    private static Vector3 CartesianSnap(Vector3 pos, Vector3 intervals)
    {
        pos.x = SnapFloat(pos.x, intervals.x);
        pos.y = SnapFloat(pos.y, intervals.y);
        pos.z = SnapFloat(pos.z, intervals.z);
        return pos;
    }

    private static float SnapFloat(float val, float interval)
    {
        return Mathf.RoundToInt(val / interval) * interval;
    }
}