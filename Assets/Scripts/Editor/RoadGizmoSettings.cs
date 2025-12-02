using UnityEditor;
using UnityEngine;

public class RoadGizmoSettings : EditorWindow
{
    private const string GIZMO_SHOW_PATH_KEY = "RoadSystem_ShowBezierPaths";
    private const string GIZMO_SHOW_NEXT_KEY = "RoadSystem_ShowNextLaneConnections";
    private const string GIZMO_SHOW_NEIGHBOR_KEY = "RoadSystem_ShowNeighborLanes";
    private const string GIZMO_SHOW_SPHERES_KEY = "RoadSystem_ShowStartEndSpheres";
    private const string GIZMO_SHOW_ARROWS_KEY = "RoadSystem_ShowDirectionArrows";
    private const string GIZMO_SHOW_TEXT_KEY = "RoadSystem_ShowSpeedLimitText";
    private const string GIZMO_SIMPLE_LINES_KEY = "RoadSystem_UseSimpleLines";
    private const string GIZMO_CULLING_DISTANCE_KEY = "RoadSystem_GizmoCullDistance";
    private const string GIZMO_ONLY_SELECTED_KEY = "RoadSystem_OnlyShowSelectedContext";
    private bool showPaths;
    private bool showNextConnections;
    private bool showNeighborConnections;
    private bool showSpheres;
    private bool showArrows;
    private bool showText;
    private bool useSimpleLines;
    private float gizmoCullDistance;
    private bool onlyShowSelectedContext;
    [MenuItem("Tools/Road System Gizmo Settings")]
    public static void ShowWindow()
    {
        GetWindow<RoadGizmoSettings>("Road Gizmos");
    }

    private void OnEnable()
    {
        showPaths = EditorPrefs.GetBool(GIZMO_SHOW_PATH_KEY, true);
        showNextConnections = EditorPrefs.GetBool(GIZMO_SHOW_NEXT_KEY, true);
        showNeighborConnections = EditorPrefs.GetBool(GIZMO_SHOW_NEIGHBOR_KEY, true);
        showSpheres = EditorPrefs.GetBool(GIZMO_SHOW_SPHERES_KEY, true);
        showArrows = EditorPrefs.GetBool(GIZMO_SHOW_ARROWS_KEY, true);
        showText = EditorPrefs.GetBool(GIZMO_SHOW_TEXT_KEY, true);
        useSimpleLines = EditorPrefs.GetBool(GIZMO_SIMPLE_LINES_KEY, false); 
        gizmoCullDistance = EditorPrefs.GetFloat(GIZMO_CULLING_DISTANCE_KEY, 1000f);
        onlyShowSelectedContext = EditorPrefs.GetBool(GIZMO_ONLY_SELECTED_KEY, false);
    }

    private void OnGUI()
    {
        GUILayout.Label("Road Gizmo Settings", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Toggle gizmos to improve editor performance", MessageType.Info);

        EditorGUI.BeginChangeCheck();

        showPaths = EditorGUILayout.Toggle("Show Lane Paths", showPaths);
        showNextConnections = EditorGUILayout.Toggle("Show Next Lane Connections", showNextConnections);
        showNeighborConnections = EditorGUILayout.Toggle("Show Neighbor Lanes", showNeighborConnections);
        showSpheres = EditorGUILayout.Toggle("Show Start/End Spheres", showSpheres);
        showArrows = EditorGUILayout.Toggle("Show Direction Arrows", showArrows);
        showText = EditorGUILayout.Toggle("Show Speed Limit Text", showText);
        useSimpleLines = EditorGUILayout.Toggle("Use Simple Lane Lines", useSimpleLines);


        onlyShowSelectedContext = EditorGUILayout.Toggle("Show Only Selected Context", onlyShowSelectedContext);
        if (onlyShowSelectedContext)
        {
            EditorGUILayout.HelpBox("Improves performance", MessageType.Info);
        }


        if (useSimpleLines)
        {
            EditorGUILayout.HelpBox("Draws straight lines", MessageType.Info);
        }
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetBool(GIZMO_SHOW_PATH_KEY, showPaths);
            EditorPrefs.SetBool(GIZMO_SHOW_NEXT_KEY, showNextConnections);
            EditorPrefs.SetBool(GIZMO_SHOW_NEIGHBOR_KEY, showNeighborConnections);
            EditorPrefs.SetBool(GIZMO_SHOW_SPHERES_KEY, showSpheres);
            EditorPrefs.SetBool(GIZMO_SHOW_ARROWS_KEY, showArrows);
            EditorPrefs.SetBool(GIZMO_SHOW_TEXT_KEY, showText);
            EditorPrefs.SetBool(GIZMO_ONLY_SELECTED_KEY, onlyShowSelectedContext);
            EditorPrefs.SetBool(GIZMO_SIMPLE_LINES_KEY, useSimpleLines);
            EditorPrefs.SetFloat(GIZMO_CULLING_DISTANCE_KEY, gizmoCullDistance);

            SceneView.RepaintAll();
        }

        EditorGUILayout.Space(5);
        gizmoCullDistance = EditorGUILayout.FloatField("Gizmo Cull Distance", gizmoCullDistance);
        EditorGUILayout.HelpBox("Set to 0 to disable culling", MessageType.Info);


    }
}