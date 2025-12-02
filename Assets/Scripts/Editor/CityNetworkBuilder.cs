using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class CityNetworkBuilder : EditorWindow
{
    //Inputs
    private RoadGraphLoader roadGraphLoader;
    private TrafficLane lanePrefab;
    private Transform networkParent;

    //Default Settings
    private float positionScale = 1.0f;
    private float defaultLaneWidth = 3.5f;
    private int defaultInLanes = 2;
    private int defaultOutLanes = 2;
    private float defaultIntersectionSize = 10.0f; 
    private float defaultStubLength = 10.0f;

    //Private vars
    private Dictionary<int, NodeData> _nodesById = new Dictionary<int, NodeData>();
    private Dictionary<int, List<EdgeData>> _edgesByNodeId = new Dictionary<int, List<EdgeData>>();
    private Dictionary<int, TrafficIntersection> _spawnedIntersections = new Dictionary<int, TrafficIntersection>();
    private List<RoadSegment> _spawnedRoads = new List<RoadSegment>();

    [MenuItem("Tools/City Network Builder")]
    public static void ShowWindow()
    {
        GetWindow<CityNetworkBuilder>("City Network Builder");
    }

    private void OnGUI()
    {
        GUILayout.Label("Master Network Generator", EditorStyles.boldLabel);

        EditorGUILayout.Space(10);
        GUILayout.Label("Required Inputs", EditorStyles.boldLabel);
        roadGraphLoader = (RoadGraphLoader)EditorGUILayout.ObjectField("Road Graph Loader (Scene)", roadGraphLoader, typeof(RoadGraphLoader), true);
        lanePrefab = (TrafficLane)EditorGUILayout.ObjectField("Lane Prefab", lanePrefab, typeof(TrafficLane), false);
        networkParent = (Transform)EditorGUILayout.ObjectField("Network Parent", networkParent, typeof(Transform), true);

        EditorGUILayout.Space(10);
        GUILayout.Label("Default Generation Settings", EditorStyles.boldLabel);
        positionScale = EditorGUILayout.FloatField("Position Scale", positionScale);
        defaultLaneWidth = EditorGUILayout.FloatField("Default Lane Width", defaultLaneWidth);
        defaultInLanes = EditorGUILayout.IntField("Default In-Lanes", defaultInLanes);
        defaultOutLanes = EditorGUILayout.IntField("Default Out-Lanes", defaultOutLanes);
        defaultIntersectionSize = EditorGUILayout.FloatField("Default Intersection Size", defaultIntersectionSize);
        defaultStubLength = EditorGUILayout.FloatField("Default Stub Length", defaultStubLength);

        EditorGUILayout.Space(10);

        bool canGenerate = roadGraphLoader != null && lanePrefab != null && networkParent != null;
        GUI.enabled = canGenerate;

        if (GUILayout.Button("Build network", GUILayout.Height(40)))
        {
            GenerateNetwork();
        }

        if (!canGenerate)
        {
            EditorGUILayout.HelpBox("Please assign all inputs", MessageType.Warning);
        }
        GUI.enabled = true;
    }

    private void GenerateNetwork()
    {
        //setup and load data
        ClearNetwork();
        if (!LoadAndProcessGraph()) return;

        //generate intersectiosns and roads in passes
        GenerateIntersections();
        GenerateRoads();

        //build the connections
        BuildNetwork();


    }

    private void ClearNetwork()
    {
        //Clear old data
        _nodesById.Clear();
        _edgesByNodeId.Clear();
        _spawnedIntersections.Clear();
        _spawnedRoads.Clear();

        for (int i = networkParent.childCount - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(networkParent.GetChild(i).gameObject);
        }
    }

    private bool LoadAndProcessGraph()
    {
        if (roadGraphLoader.Graph == null) roadGraphLoader.LoadGraphData();

        var rawData = roadGraphLoader.RawData;
        if (rawData?.nodes == null || rawData.edges == null)
        {
            Debug.LogError("Builder: RoadGraphLoader has no data.", roadGraphLoader);
            return false;
        }

        foreach (var node in rawData.nodes)
        {
            _nodesById[node.id] = node;
            _edgesByNodeId[node.id] = new List<EdgeData>();
        }

        foreach (var edge in rawData.edges)
        {
            if (_edgesByNodeId.ContainsKey(edge.source))
            {
                _edgesByNodeId[edge.source].Add(edge);
            }
        }
        return true;
    }

    private void GenerateIntersections()
    {
        foreach (var node in _nodesById.Values)
        {
            //Create
            Vector3 nodePos = ToWorld(node.pos);
            GameObject intGO = new GameObject($"Intersection_{node.id}");
            intGO.transform.SetParent(networkParent);
            intGO.transform.position = nodePos;
            Undo.RegisterCreatedObjectUndo(intGO, "Create Intersection");

            //Configure
            TrafficIntersection newIntersection = intGO.AddComponent<TrafficIntersection>();
            newIntersection.lanePrefab = this.lanePrefab;
  
            //fuind and create connections
            List<EdgeData> outgoingEdges = _edgesByNodeId[node.id];
            List<NodeData> connectedNodes = new List<NodeData>();

            foreach (var edge in outgoingEdges)
            {
                if (_nodesById.TryGetValue(edge.target, out NodeData targetNode))
                {
                    connectedNodes.Add(targetNode);
                }
            }

            foreach (var edge in roadGraphLoader.RawData.edges)
            {
                if (edge.target == node.id)
                {
                    if (_nodesById.TryGetValue(edge.source, out NodeData sourceNode))
                    {
                        if (!connectedNodes.Contains(sourceNode))
                        {
                            connectedNodes.Add(sourceNode);
                        }
                    }
                }
            }


            foreach (var otherNode in connectedNodes.Distinct())
            {
                Vector3 dir = (ToWorld(otherNode.pos) - nodePos).normalized;
                dir.y = 0; 
                float angle = Vector3.SignedAngle(Vector3.forward, dir, Vector3.up);

                newIntersection.connectors.Add(new IntersectionConnector
                {
                    name = $"Road_To_{otherNode.id}", 
                    rotation = angle,
                    laneWidth = this.defaultLaneWidth,
                    inLanes = this.defaultInLanes,
                    outLanes = this.defaultOutLanes
                });
            }

            _spawnedIntersections[node.id] = newIntersection;
        }
    }

    private void GenerateRoads()
    {
        //Generate road segemnts
        HashSet<string> processedEdges = new HashSet<string>();

        foreach (var edge in roadGraphLoader.RawData.edges)
        {
            string keyA = $"{edge.source}_{edge.target}";
            string keyB = $"{edge.target}_{edge.source}";

            if (processedEdges.Contains(keyA) || processedEdges.Contains(keyB))
                continue;

            //Get the two intersections to connect
            if (_spawnedIntersections.TryGetValue(edge.source, out TrafficIntersection intA) &&
                _spawnedIntersections.TryGetValue(edge.target, out TrafficIntersection intB))
            {
                //create
                GameObject roadGO = new GameObject($"Road_{edge.source}_to_{edge.target}");
                roadGO.transform.SetParent(networkParent);
                roadGO.transform.position = (intA.transform.position + intB.transform.position) / 2f;
                Undo.RegisterCreatedObjectUndo(roadGO, "Create Road Segment");

                //config
                RoadSegment newRoad = roadGO.AddComponent<RoadSegment>();
                newRoad.lanePrefab = this.lanePrefab;
                newRoad.intersectionA = intA;
                newRoad.intersectionB = intB;

                newRoad.connectorAName = $"Road_To_{edge.target}";
                newRoad.connectorBName = $"Road_To_{edge.source}";

                _spawnedRoads.Add(newRoad);
                processedEdges.Add(keyA); 
            }
        }
    }

    private void BuildNetwork()
    {

        //Builds intersections
        foreach (var intersection in _spawnedIntersections.Values)
        {
            Undo.RecordObject(intersection, "Build Intersection");
            intersection.RebuildIntersection();
        }

        //Builds roads
        foreach (var road in _spawnedRoads)
        {
            Undo.RecordObject(road, "Build Road Segment");
            road.RebuildRoad();
        }
    }

    /// <summary>
    /// Converts CityEngine 3D-array coordinates to a Unity Vector3.
    /// </summary>
    private Vector3 ToWorld(float[] coords)
    {
        if (coords == null || coords.Length < 3)
            return Vector3.zero;


        Vector3 localPos = new Vector3(-coords[0], coords[1], coords[2]) * positionScale;

        return localPos;
    }
}