#region

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using HandleUtility = UnityEditor.HandleUtility;

#endregion

namespace EditorTools
{
    public struct RandomSpawnData
    {
        public Vector2 position;
        public Vector3 rotation;
        public GameObject spawnPrefab;
    }

    public class SpawnPoint
    {
        public Vector3 position;
        public Quaternion rotation;
        public RandomSpawnData spawnData;
        public bool isValid;

        public Vector3 Up => rotation * Vector3.up;

        public SpawnPoint(Vector3 position, Quaternion rotation, RandomSpawnData spawnData)
        {
            this.position = position;
            this.rotation = rotation;
            this.spawnData = spawnData;

            // Check if mesh fits at this position
            if (spawnData.spawnPrefab != null)
            {
                var spawnable = spawnData.spawnPrefab.GetComponent<SpawnablePrefab>();
                if (spawnable == null)
                {
                    isValid = true;
                }
                else
                {
                    float h = spawnable.height;
                    Ray heightRaycast = new Ray(position, Up);
                    isValid = !Physics.Raycast(heightRaycast, h);
                }
            }
        }
    }

    public class PropCannon : EditorWindow
    {
        [MenuItem("Tools/Prop Cannon")]
        private static void OpenPropCannon() => GetWindow<PropCannon>("Prop Cannon");

        public float radius;
        public int spawnCount;

        public bool[] prefabSelectionState;
        public List<GameObject> spawnPrefabs;
        public Material invalidMaterial;

        private SerializedObject so;
        private SerializedProperty propRadius;
        private SerializedProperty propSpawnCount;

        private RandomSpawnData[] randomSpawnDatas;
        private GameObject[] prefabs;

        private void OnEnable()
        {
            so = new SerializedObject(this);
            propRadius = so.FindProperty("radius");
            propSpawnCount = so.FindProperty("spawnCount");

            Shader sh = Shader.Find("Unlit/HologramShader");
            invalidMaterial = new Material(sh);

            FindPrefabs();
            GenerateSpawnData(spawnPrefabs);

            SceneView.duringSceneGui += DuringSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DuringSceneGUI;
        }

        private void FindPrefabs()
        {
            string[] guids = AssetDatabase.FindAssets("t:prefab", new[] { "Assets/Prefabs" });
            var paths = guids.Select(AssetDatabase.GUIDToAssetPath);
            paths.ToList().ForEach(a => Debug.Log(a));
            prefabs = paths.Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();
            prefabSelectionState = new bool[prefabs.Length];
            UpdateSpawnPrefabs();
        }

        private void GenerateSpawnData(List<GameObject> possibleSpawnPrefabs)
        {
            randomSpawnDatas = new RandomSpawnData[spawnCount];
            for (int i = 0; i < spawnCount; i++)
            {
                randomSpawnDatas[i].position = Random.insideUnitCircle * radius;
                randomSpawnDatas[i].rotation = Random.onUnitSphere;
                randomSpawnDatas[i].spawnPrefab = possibleSpawnPrefabs.Count == 0 ? null : possibleSpawnPrefabs[Random.Range(0, possibleSpawnPrefabs.Count)];
            }
        }

        private void OnGUI()
        {
            so.Update();

            // Editor fields
            EditorGUILayout.PropertyField(propRadius);
            EditorGUILayout.PropertyField(propSpawnCount);

            // Set bounds for some values
            propRadius.floatValue = propRadius.floatValue.AtLeast(0.25f);
            propSpawnCount.intValue = propSpawnCount.intValue.AtLeast(1);

            // Regenerate spawn orientations
            if (so.ApplyModifiedProperties())
            {
                GenerateSpawnData(spawnPrefabs);
                SceneView.RepaintAll();
            }

            // Deselect field when left clicking on window
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                GUI.FocusControl(null);
                Repaint();
            }
        }

        private void DuringSceneGUI(SceneView view)
        {
            DrawPrefabSelectionButtons();

            Handles.zTest = CompareFunction.LessEqual;
            var camTransform = view.camera.transform;

            // Scroll to change radius
            if ((Event.current.modifiers & EventModifiers.Alt) != 0 && Event.current.type == EventType.ScrollWheel)
            {
                float dir = Event.current.delta.y;
                ScrollUpdateRadius(dir);
                Event.current.Use();
            }

            // Repaint on mouse movement
            if (Event.current.type == EventType.MouseMove)
            {
                SceneView.RepaintAll();
            }

            // See if aiming at valid point
            if (RaycastFromCamera(camTransform.up, out Matrix4x4 tangentToWorldMtx))
            {
                var spawnPoints = GetSPawnPoints(tangentToWorldMtx);

                if (Event.current.type == EventType.Repaint)
                {
                    DrawPlacementCircle(tangentToWorldMtx);
                    DrawSpawnPlacementPreview(spawnPoints);
                }

                // Spawn Objects
                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Space)
                {
                    Event.current.Use();
                    if (TrySpawnObjects(spawnPoints))
                    {
                        GenerateSpawnData(spawnPrefabs);
                        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                    }
                }
            }
        }

        private void ScrollUpdateRadius(float dir)
        {
            radius *= 1 + 0.1f * dir;
            GenerateSpawnData(spawnPrefabs);
            SceneView.RepaintAll();
            Repaint();
        }

        private bool RaycastFromCamera(Vector3 cameraUp, out Matrix4x4 tangentToWorldMtx)
        {
            var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            Vector3 test = Vector3.Cross(cameraUp, ray.direction);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Set up tangent space
                Vector3 normal = hit.normal;
                Handles.DrawLine(hit.point, hit.point + test * 2f);
                Vector3 tangent = Vector3.Cross(test, normal).normalized;
                Vector3 bitangent = Vector3.Cross(normal, tangent);
                tangentToWorldMtx = Matrix4x4.TRS(hit.point, Quaternion.LookRotation(normal, tangent), Vector3.one);
                return true;
            }

            tangentToWorldMtx = default;
            return false;
        }

        private void DrawPrefabSelectionButtons()
        {
            Handles.BeginGUI();
            Rect rect = new Rect(8, 8, 64, 64);
            for (int i = 0; i < prefabs.Length; i++)
            {
                var prefab = prefabs[i];
                Texture icon = AssetPreview.GetAssetPreview(prefab);

                EditorGUI.BeginChangeCheck();
                prefabSelectionState[i] = GUI.Toggle(rect, prefabSelectionState[i], icon);
                if (EditorGUI.EndChangeCheck())
                {
                    UpdateSpawnPrefabs();
                    GenerateSpawnData(spawnPrefabs);
                    Repaint();
                }

                rect.y += rect.height + 2;
            }

            Handles.EndGUI();
        }

        private void UpdateSpawnPrefabs()
        {
            spawnPrefabs.Clear();
            for (int j = 0; j < prefabSelectionState.Length; j++)
            {
                if (prefabSelectionState[j])
                {
                    spawnPrefabs.Add(prefabs[j]);
                }
            }
        }

        private List<SpawnPoint> GetSPawnPoints(Matrix4x4 tangentToWorldMtx)
        {
            var spawnPoses = new List<SpawnPoint>();
            foreach (var spawnData in randomSpawnDatas)
            {
                Matrix4x4 randOrientationToTangentMtx = Matrix4x4.Translate(spawnData.position);
                var placeRay = TangentMatrixToRay(tangentToWorldMtx * randOrientationToTangentMtx);
                if (Physics.Raycast(placeRay, out RaycastHit ptHit))
                {
                    // Generate random rotation around up vector plane
                    Vector3 forwards = Vector3.Cross(spawnData.rotation, ptHit.normal);
                    Quaternion baseRotation = Quaternion.LookRotation(forwards, ptHit.normal);
                    var spawnPoint = new SpawnPoint(ptHit.point, baseRotation, spawnData);
                    spawnPoses.Add(spawnPoint);
                }
            }

            return spawnPoses;
        }

        private void DrawPlacementCircle(Matrix4x4 tangentToWorldMtx)
        {
            Vector3 position = tangentToWorldMtx.GetPosition();
            // Draw tangent space axis
            Handles.color = Color.blue;
            Handles.DrawAAPolyLine(position, position + tangentToWorldMtx.MultiplyVector(Vector3.forward));
            Handles.color = Color.red;
            Handles.DrawAAPolyLine(position, position + tangentToWorldMtx.MultiplyVector(Vector3.right));
            Handles.color = Color.green;
            Handles.DrawAAPolyLine(position, position + tangentToWorldMtx.MultiplyVector(Vector3.up));

            // Draw spawn radius circle
            int circleResolution = 100;
            Vector3[] circlePoints = new Vector3[circleResolution + 1];
            for (int i = 0; i <= circleResolution; i++)
            {
                float tau = Mathf.PI * 2;
                float angle = (float)i / circleResolution * tau;
                Vector2 circlePoint = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                // Raycast
                Ray circleRay = TangentMatrixToRay(tangentToWorldMtx * Matrix4x4.Translate(circlePoint));
                if (Physics.Raycast(circleRay, out RaycastHit circleHit))
                {
                    circlePoints[i] = circleHit.point + circleHit.normal * 0.15f;
                }
                else
                {
                    circlePoints[i] = circleRay.origin + circleRay.direction * 3;
                }
            }

            Handles.color = Color.white;
            Handles.DrawAAPolyLine(circlePoints);
        }

        private void DrawSpawnPlacementPreview(List<SpawnPoint> spawnPoints)
        {
            foreach (var spawnPoint in spawnPoints)
            {
                // Draw point handles
                Handles.color = Color.magenta;
                Handles.DrawAAPolyLine(spawnPoint.position, spawnPoint.position + spawnPoint.Up);
                DrawSphere(spawnPoint.position, 0.1f);
                var spawnPrefab = spawnPoint.spawnData.spawnPrefab;
                // mesh
                if (spawnPrefab != null)
                {
                    Matrix4x4 poseToWorld = Matrix4x4.TRS(spawnPoint.position, spawnPoint.rotation, Vector3.one);
                    foreach (var filter in spawnPrefab.GetComponentsInChildren<MeshFilter>())
                    {
                        if (filter.sharedMesh == null) continue;
                        Matrix4x4 localToPose = filter.transform.localToWorldMatrix;

                        var mesh = filter.sharedMesh;
                        var previewMaterial = spawnPoint.isValid
                            ? filter.GetComponent<MeshRenderer>().sharedMaterial
                            : invalidMaterial;
                        Graphics.DrawMesh(mesh, poseToWorld * localToPose, previewMaterial, 0, Camera.current);
                    }

                    foreach (var filter in spawnPrefab.GetComponentsInChildren<ProBuilderMesh>())
                    {
                        Matrix4x4 localToPose = filter.transform.localToWorldMatrix;

                        var mesh = ProBuilderToMesh(filter);
                        var previewMaterial = spawnPoint.isValid
                            ? filter.GetComponent<MeshRenderer>().sharedMaterial
                            : invalidMaterial;
                        Graphics.DrawMesh(mesh, poseToWorld * localToPose, previewMaterial, 0, Camera.current);
                    }
                }
            }
        }

        private Mesh ProBuilderToMesh(ProBuilderMesh filter)
        {
            var mesh = new Mesh();
            var vertices = filter.GetVertices().Select(o => new Vector3(
                o.position.x,
                o.position.y,
                o.position.z
            )).ToList();
            mesh.SetVertices(vertices);
            var indices = new List<int>();
            filter.faces.ToList().ForEach(face =>
                indices.AddRange(face.indexes)
            );
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        private Ray TangentMatrixToRay(Matrix4x4 matrix)
        {
            Vector3 position = matrix.GetPosition();
            Vector3 normal = matrix.GetColumn(2).normalized;
            position += 3 * normal;
            return new Ray(position, -normal);
        }

        private void DrawSphere(Vector3 pos, float radius)
        {
            Handles.SphereHandleCap(-1, pos, Quaternion.identity, radius, EventType.Repaint);
        }

        private bool TrySpawnObjects(List<SpawnPoint> spawnPoints)
        {
            foreach (var spawnPoint in spawnPoints)
            {
                var spawnPrefab = spawnPoint.spawnData.spawnPrefab;
                if (spawnPrefab != null && spawnPoint.isValid)
                {
                    GameObject spawnedObject = (GameObject)PrefabUtility.InstantiatePrefab(spawnPrefab);
                    Undo.RegisterCreatedObjectUndo(spawnedObject, "Prefab Cannon Place");
                    spawnedObject.transform.position = spawnPoint.position;
                    spawnedObject.transform.rotation = spawnPoint.rotation;
                }
            }

            return true;
        }
    }
}