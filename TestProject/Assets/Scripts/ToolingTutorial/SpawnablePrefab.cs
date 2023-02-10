#region

using UnityEditor;
using UnityEngine;

#endregion

public class SpawnablePrefab : MonoBehaviour
{
    public float height = 1f;

    private void OnDrawGizmos()
    {
        Vector3 a = transform.position;
        Vector3 b = transform.position + transform.up * height;
        Handles.DrawAAPolyLine(a, b);

        void DrawSphere(Vector3 pos, float size)
        {
            Gizmos.DrawSphere(pos, HandleUtility.GetHandleSize(pos) * size);
        }

        float size = 0.2f;
        DrawSphere(a, size);
        DrawSphere(b, size);
    }
}