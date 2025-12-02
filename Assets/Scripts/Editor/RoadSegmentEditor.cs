using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// Road segment editor script
/// </summary>
[CustomEditor(typeof(RoadSegment))]
public class RoadSegmentEditor : Editor
{
    private RoadSegment _target;

    private void OnEnable()
    {
        _target = (RoadSegment)target;
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("intersectionA"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("intersectionB"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("lanePrefab"));

        EditorGUILayout.Space(5);

        bool canAutoDetect = _target.intersectionA != null && _target.intersectionB != null;

        GUI.enabled = canAutoDetect; 
        if (GUILayout.Button("AutoDetect Connectors"))
        {
            Undo.RecordObject(_target, "AutoDetect Connectors");
            _target.AutoDetectConnectors();
            EditorUtility.SetDirty(_target); 
        }
        GUI.enabled = true; 

        if (!canAutoDetect)
        {
            EditorGUILayout.HelpBox("Assign both intersections", MessageType.Info);
        }



        if (_target.intersectionA != null)
        {
            string[] connectorNamesA = _target.intersectionA.connectors.Select(c => c.name).ToArray();
            if (connectorNamesA.Length > 0)
            {
                int currentIndex = System.Array.IndexOf(connectorNamesA, _target.connectorAName);
                if (currentIndex < 0) currentIndex = 0;

                int newIndex = EditorGUILayout.Popup("Connector A", currentIndex, connectorNamesA);
                if (newIndex != currentIndex || _target.connectorAName != connectorNamesA[newIndex])
                {
                    Undo.RecordObject(_target, "Change Connector A");
                    _target.connectorAName = connectorNamesA[newIndex];
                    EditorUtility.SetDirty(_target);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Intersection A has no connectors.", MessageType.Warning);
            }
        }

        if (_target.intersectionB != null)
        {
            string[] connectorNamesB = _target.intersectionB.connectors.Select(c => c.name).ToArray();
            if (connectorNamesB.Length > 0)
            {
                int currentIndex = System.Array.IndexOf(connectorNamesB, _target.connectorBName);
                if (currentIndex < 0) currentIndex = 0;

                int newIndex = EditorGUILayout.Popup("Connector B", currentIndex, connectorNamesB);
                if (newIndex != currentIndex || _target.connectorBName != connectorNamesB[newIndex])
                {
                    Undo.RecordObject(_target, "Change ConnectorB");
                    _target.connectorBName = connectorNamesB[newIndex];
                    EditorUtility.SetDirty(_target);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Intersection B has no connectors.", MessageType.Warning);
            }
        }

        EditorGUILayout.Space(10);
        if (GUILayout.Button("RebuildRoad Segment", GUILayout.Height(30)))
        {
            foreach (var t in _target.GetComponentsInChildren<Transform>())
            {
                Undo.RecordObject(t, "Rebuild Road Segment");
            }
            Undo.RecordObject(_target, "Rebuild Road Segment");

            _target.RebuildRoad();
        }

        serializedObject.ApplyModifiedProperties();
    }
}