using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TrafficIntersection))]
/// <summary>
/// Handler for the TrafficIntersection component in the Unity Editor.
/// </summary>
public class TrafficIntersectionEditor : Editor
{
    private TrafficIntersection _target;

    private void OnEnable()
    {
        _target = (TrafficIntersection)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Rebuild Intersection", GUILayout.Height(30)))
        {
            if (_target.lanePrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Lane Prefab", "OK");
            }
            else
            {
                Undo.RecordObject(_target, "Rebuild Intersection");
                foreach (var t in _target.GetComponentsInChildren<Transform>())
                {
                    Undo.RecordObject(t, "Rebuild Intersection");
                }

                _target.RebuildIntersection();
            }
        }
    }

    private void OnSceneGUI()
    {
        if (_target == null) return;

        bool hasChanged = false;

        foreach (var connector in _target.connectors)
        {
            Quaternion handleRot = Quaternion.Euler(0, connector.rotation, 0);
            handleRot = _target.transform.rotation * handleRot;
            Vector3 handlePos = _target.transform.position;

            Handles.color = new Color(1, 0.8f, 0, 0.7f);
            EditorGUI.BeginChangeCheck();

            handleRot = Handles.RotationHandle(handleRot, handlePos);

            if (EditorGUI.EndChangeCheck())
            {
                hasChanged = true;
                Undo.RecordObject(_target, "Rotate Intersection Connector");
                Quaternion localRot = Quaternion.Inverse(_target.transform.rotation) * handleRot;
                connector.rotation = localRot.eulerAngles.y;
            }
        }

        if (hasChanged)
        {
            _target.RebuildIntersection();
        }
    }
}