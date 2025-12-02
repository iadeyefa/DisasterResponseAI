using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class RoadSystemEditor : EditorWindow
{
    // --- Preferences ---
    private TrafficLane lanePrefab;
    private float laneWidth = 3.5f;
    private float roadLength = 50.0f;
    private float intersectionSize = 15.0f;
    private float stubLength = 10.0f;

    // --- Gizmo Toggle ---
    private bool showExtraGizmos;
    private const string GIZMO_PREF_KEY = "RoadSystem_ShowExtraGizmos";

    [MenuItem("Tools/Road System Editor (Manual)")]
    public static void ShowWindow()
    {
        GetWindow<RoadSystemEditor>("Road System");
    }

    private void OnEnable()
    {
        showExtraGizmos = EditorPrefs.GetBool(GIZMO_PREF_KEY, true);
    }

    private void OnGUI()
    {
        GUILayout.Label("Road Generation Settings", EditorStyles.boldLabel);
        lanePrefab = (TrafficLane)EditorGUILayout.ObjectField("Lane Prefab", lanePrefab, typeof(TrafficLane), false);
        laneWidth = EditorGUILayout.FloatField("Lane Width", laneWidth);
        roadLength = EditorGUILayout.FloatField("Road Length", roadLength);
        intersectionSize = EditorGUILayout.FloatField("Intersection Size (Inner)", intersectionSize);
        stubLength = EditorGUILayout.FloatField("Connection Stub Length", stubLength);
        EditorGUILayout.Space(10);
        showExtraGizmos = EditorGUILayout.Toggle("Show Lane Extras", showExtraGizmos);

        // --- SECTION: Generators ---
        if (lanePrefab == null)
        {
            EditorGUILayout.HelpBox("Please assign a Lane Prefab to enable generation.", MessageType.Warning);
            GUI.enabled = false;
        }

        GUILayout.Label("Generators", EditorStyles.boldLabel);

        //Profile roads
        if (GUILayout.Button("Create 2-Way Street")) CreateTwoWayStreet();
        if (GUILayout.Button("Create T-Intersection")) CreateTIntersection();
        if (GUILayout.Button("Create 4-Way Intersection")) CreateFourWayIntersection();

        GUI.enabled = true;
        EditorGUILayout.Space(10);


        GUILayout.Label("Utilities", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Select two lanes. The tool will auto-detect direction and bridge them.", MessageType.Info);
        if (GUILayout.Button("Bridge 2 Selected Lanes"))
        {
            BridgeSelectedLanes();
        }
    }

    private Vector3 GetSpawnPoint()
    {
        return Vector3.zero; 
    }

 
    private void CreateTwoWayStreet()
    {
        Vector3 spawnPoint = GetSpawnPoint();
        GameObject roadRoot = new GameObject("2-Way Street");
        roadRoot.transform.position = spawnPoint;
        Undo.RegisterCreatedObjectUndo(roadRoot, "Create 2-Way Street");

        float halfWidth = laneWidth / 2.0f;

        RoadFactory.CreateLane(roadRoot.transform, lanePrefab, "Lane_A",
            new Vector3(-halfWidth, 0, 0), new Vector3(-halfWidth, 0, roadLength * 0.33f),
            new Vector3(-halfWidth, 0, roadLength * 0.66f), new Vector3(-halfWidth, 0, roadLength));

        RoadFactory.CreateLane(roadRoot.transform, lanePrefab, "Lane_B",
            new Vector3(halfWidth, 0, roadLength), new Vector3(halfWidth, 0, roadLength * 0.66f),
            new Vector3(halfWidth, 0, roadLength * 0.33f), new Vector3(halfWidth, 0, 0));

        Selection.activeGameObject = roadRoot;
    }
    private void CreateFourWayIntersection()
    {
        Vector3 spawnPoint = GetSpawnPoint();
        GameObject intRoot = new GameObject("4-Way Intersection");
        intRoot.transform.position = spawnPoint;
        Undo.RegisterCreatedObjectUndo(intRoot, "Create 4-Way Intersection");

        RoadFactory.CreateFourWayIntersection(intRoot.transform, lanePrefab, intersectionSize, laneWidth, stubLength);

        Selection.activeGameObject = intRoot;
    }

    private void CreateTIntersection()
    {
        //Vector3 spawnPoint = GetSpawnPoint();
        //GameObject intRoot = new GameObject("T-Intersection");
        //intRoot.transform.position = spawnPoint;
        //Undo.RegisterCreatedObjectUndo(intRoot, "Create T-Intersection");

        // Call the factory
        //RoadFactory.CreateTIntersection(intRoot.transform, lanePrefab, intersectionSize, laneWidth, stubLength);

        //Selection.activeGameObject = intRoot;
    }

    private void BridgeSelectedLanes()
    {
        List<TrafficLane> selectedLanes = Selection.gameObjects
            .Select(go => go.GetComponent<TrafficLane>())
            .Where(lane => lane != null)
            .ToList();

        if (selectedLanes.Count != 2)
        {
            EditorUtility.DisplayDialog("Connection Error", "Please select 2 TrafficLane", "OK");
            return;
        }

        TrafficLane laneA = selectedLanes[0];
        TrafficLane laneB = selectedLanes[1];

        //Auto-detect direction
        float distA_to_B = Vector3.Distance(laneA.GetWorldPoint(1), laneB.GetWorldPoint(0));
        float distB_to_A = Vector3.Distance(laneB.GetWorldPoint(1), laneA.GetWorldPoint(0));

        TrafficLane fromLane, toLane;
        if (distA_to_B < distB_to_A) { fromLane = laneA; toLane = laneB; }
        else { fromLane = laneB; toLane = laneA; }

        TrafficLane newLane = RoadFactory.BridgeLanes(fromLane, toLane, fromLane.transform.parent, lanePrefab);

        if (newLane != null)
        {
            Selection.activeGameObject = newLane.gameObject;
        }
    }

}