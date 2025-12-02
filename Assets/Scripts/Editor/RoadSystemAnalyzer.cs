using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
/// <summary>
/// A tool to analyze the road network and notify of any problems
/// </summary>
public class RoadSystemAnalyzer : EditorWindow
{
    public GameObject trafficLightPrefab; 
    private Vector2 scrollPos;
    // Updated lists to store the lane along with the message
    private List<KeyValuePair<string, TrafficLane>> errors = new List<KeyValuePair<string, TrafficLane>>();
    private List<KeyValuePair<string, TrafficLane>> warnings = new List<KeyValuePair<string, TrafficLane>>();
    private List<string> graphInfo = new List<string>();

    [MenuItem("Tools/Road System Analyzer")]

    /// <summary>
    /// Show the window
    /// </summary>
    public static void ShowWindow()
    {
        GetWindow<RoadSystemAnalyzer>("Road Analyzer");
    }
    /// <summary>
    /// Setup and display the UI in Editor
    /// </summary>
    private void OnGUI()
    {
        GUILayout.Label("Road Network Analysis Tool", EditorStyles.boldLabel);

        //Analyze network button
        if (GUILayout.Button("Analyze Entire Road Network", GUILayout.Height(40)))
        {
            AnalyzeNetwork();
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        if (graphInfo.Count > 0)
        {
            GUILayout.Label("Connectivity Analysis", EditorStyles.boldLabel);
            foreach (var info in graphInfo)
            {
                EditorGUILayout.HelpBox(info, MessageType.Info);
            }
        }

        //Errors section
        if (errors.Count > 0)
        {
            GUILayout.Label("Errors (Must Fix)", EditorStyles.boldLabel);
            foreach (var entry in errors)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox(entry.Key, MessageType.Error, true);

                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    if (entry.Value != null)
                    {
                        Selection.activeGameObject = entry.Value.gameObject;
                        EditorGUIUtility.PingObject(entry.Value.gameObject);
                        if (SceneView.lastActiveSceneView != null)
                        {

                            Bounds bounds = new Bounds(entry.Value.transform.position, Vector3.one * 10f);
                            SceneView.lastActiveSceneView.Frame(bounds, false);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        //Warnings section
        if (warnings.Count > 0)
        {
            GUILayout.Label("Warnings (Review)", EditorStyles.boldLabel);
            foreach (var entry in warnings)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox(entry.Key, MessageType.Warning, true);

                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    if (entry.Value != null)
                    {
                        Selection.activeGameObject = entry.Value.gameObject;
                        EditorGUIUtility.PingObject(entry.Value.gameObject);
                        if (SceneView.lastActiveSceneView != null)
                        {
                            Bounds bounds = new Bounds(entry.Value.transform.position, Vector3.one * 10f);
                            SceneView.lastActiveSceneView.Frame(bounds, false);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndScrollView();
    }
    /// <summary>
    /// Analyze the entire network - called from the editor button
    /// </summary>
    private void AnalyzeNetwork()
    {
        //Clear messages
        errors.Clear();
        warnings.Clear();
        graphInfo.Clear();
        Debug.Log("Starting Analysis");

        // Find all lanes in the scene
        TrafficLane[] allLanes = FindObjectsOfType<TrafficLane>();
        TrafficIntersection[] allTrafficIntersections = FindObjectsOfType<TrafficIntersection>();
        if (allLanes.Length == 0)
        {
            warnings.Add(new KeyValuePair<string, TrafficLane>("No TrafficLanes found in the scene.", null));
            return;
        }

        HashSet<TrafficLane> allLanesSet = new HashSet<TrafficLane>(allLanes);
        HashSet<TrafficLane> visitedLanes = new HashSet<TrafficLane>();
        List<HashSet<TrafficLane>> graphs = new List<HashSet<TrafficLane>>();


        GameObject trafficPrefab = null;

        GameObject[] selectedObjects = Selection.gameObjects;
     


        //foreach (TrafficIntersection inter in allTrafficIntersections)
        //{
        //    inter.RebuildIntersection();
        //}
        //RoadSegment[] segs = FindObjectsOfType<RoadSegment>();
        //for (int i = 0; i < segs.Length; i++)
        //{
        //    segs[i].RebuildRoad();

        //}


        //foreach (TrafficIntersection inter in allTrafficIntersections)
        //{
        //    if(inter.connectors.Count == 1 && inter.connectors[0].incomingStubs.Count == 1 && inter.connectors[0].outgoingStubs.Count == 1)
        //    {
        //        inter.connectors[0].incomingStubs[0].nextLanes = new List<TrafficLane> { inter.connectors[0].outgoingStubs[0] };
        //        inter.connectors[0].outgoingStubs[0].previousLanes = new List<TrafficLane> { inter.connectors[0].incomingStubs[0] };
        //    }


        //}


        //Check each lane for issues
        allLanes = FindObjectsOfType<TrafficLane>();
        foreach (TrafficLane lane in allLanes)
        {
            //lane.transform.localPosition = new Vector3(lane.transform.localPosition.x, 0, lane.transform.localPosition.z);

            //TrafficIntersection inter = lane.transform.parent.GetComponent<TrafficIntersection>();
            //if(inter == null)
            //{
            //    //lane.transform.parent.transform.localPosition = new Vector3(lane.transform.parent.transform.localPosition.x, 0, lane.transform.parent.transform.localPosition.z);

            //}
            //Vector3 worldP0 = lane.transform.TransformPoint(lane.p0);
            //Vector3 worldP1 = lane.transform.TransformPoint(lane.p1);
            //Vector3 worldP2 = lane.transform.TransformPoint(lane.p2);
            //Vector3 worldP3 = lane.transform.TransformPoint(lane.p3);

            //// 2. Raycast using World coordinates
            //worldP0 = GetSnappedPoint(worldP0);
            //worldP1 = GetSnappedPoint(worldP1);
            //worldP2 = GetSnappedPoint(worldP2);
            //worldP3 = GetSnappedPoint(worldP3);

            //// 3. Convert World back to Local and save
            //lane.p0 = lane.transform.InverseTransformPoint(worldP0);
            //lane.p1 = lane.transform.InverseTransformPoint(worldP1);
            //lane.p2 = lane.transform.InverseTransformPoint(worldP2);
            //lane.p3 = lane.transform.InverseTransformPoint(worldP3);
            //lane.p0 = new Vector3(lane.p0.x, 0, lane.p0.z);
            //lane.p1 = new Vector3(lane.p1.x, 0, lane.p1.z);
            //lane.p2 = new Vector3(lane.p2.x, 0, lane.p2.z);
            //lane.p3 = new Vector3(lane.p3.x, 0, lane.p3.z);
            CheckLane(lane);
        }

        //TrafficLightGroup[] lights = FindObjectsOfType<TrafficLightGroup>();
        //foreach (TrafficLightGroup light in lights)
        //{
        //    for (int i = 0; i < light.lightGroups.Count; i++)
        //    {
        //        for (int j = 0; j < light.lightGroups[i].onTrafficLights.Length; j++)
        //        {
        //            if(light.lightGroups[i].lanes[0] == null)
        //            {
        //                continue;
        //            }

        //            light.lightGroups[i].onTrafficLights[j].transform.position
        //                = new Vector3(light.lightGroups[i].onTrafficLights[j].transform.position.x,
        //                light.lightGroups[i].lanes[0].GetWorldPoint(0).y,
        //                light.lightGroups[i].onTrafficLights[j].transform.position.z);
        //        }
        //    }
        //}


        //Check for disconnected graphs
        foreach (TrafficLane lane in allLanes)
        {
            if (!visitedLanes.Contains(lane))
            {
                //Start a new graph traversal.
                HashSet<TrafficLane> newGraph = new HashSet<TrafficLane>();
                Stack<TrafficLane> stack = new Stack<TrafficLane>();

                stack.Push(lane);
                visitedLanes.Add(lane);
                newGraph.Add(lane);

                //Perform a DFS to find all reachable lanes
                while (stack.Count > 0)
                {
                    TrafficLane current = stack.Pop();

                    //Check next lanes
                    if (current.nextLanes != null)
                    {
                        foreach (var next in current.nextLanes)
                        {
                            if (next != null && !visitedLanes.Contains(next))
                            {
                                visitedLanes.Add(next);
                                newGraph.Add(next);
                                stack.Push(next);
                            }
                        }
                    }

                    //Checkprevious lanes
                    if (current.previousLanes != null)
                    {
                        foreach (var prev in current.previousLanes)
                        {
                            if (prev != null && !visitedLanes.Contains(prev))
                            {
                                visitedLanes.Add(prev);
                                newGraph.Add(prev);
                                stack.Push(prev);
                            }
                        }
                    }
                }
                graphs.Add(newGraph);
            }
        }

        //Report graph analysis
        graphInfo.Add($"Found {allLanes.Length} total lanes.");
        graphInfo.Add($"Network is divided into {graphs.Count} discnnected graph(s).");

        if (graphs.Count > 1)
        {
            for (int i = 0; i < graphs.Count; i++)
            {
                graphInfo.Add($"  Graph  {i + 1} contains {graphs[i].Count} lanes.");
                if (graphs[i].Count > 0)
                {
                    Debug.LogWarning($"Graph {i + 1}  Your vehicle cannot route from this graph to another.", graphs[i].First());
                }
            }
        }
        else
        {
            graphInfo.Add("Your road network is fully connected. Good job!");
        }

        //Summary
        string summary = $"Analysis Complete: Found {errors.Count} Errors, {warnings.Count} Warnings, and {graphs.Count} disconnected graphs.";
        Debug.Log(summary);

        //Refresh window
        Repaint();
    }
    /// <summary>
    /// Checks a lane for errors or warnings
    /// </summary>
    private void CheckLane(TrafficLane lane)
    {
        if (lane == null) return;

        //Check for dead end lanes - all lanes should have a next lane or a neighbor lane
        if (lane.nextLanes != null)
        {
            for (int i = 0; i < lane.nextLanes.Count; i++)
            {
                if (lane.nextLanes[i] == null)
                {
                    string error = $"Lane '{lane.name}' has a NULL entry in its 'Next Lanes' list at index {i}. This will break pathfinding!";
                    // Add to new list structure
                    errors.Add(new KeyValuePair<string, TrafficLane>(error, lane));
                    Debug.LogError(error, lane);
                }
            }
        }

        //Dead end lane
        if (lane.nextLanes == null || lane.nextLanes.Count == 0)
        {
            string warning = $"Lane '{lane.name}' is a DEAD END (no 'Next Lanes'). Is this intentional?";
            warnings.Add(new KeyValuePair<string, TrafficLane>(warning, lane));
            Debug.LogWarning(warning, lane);
        }

        //No previous lanes connected
        if (lane.previousLanes == null || lane.previousLanes.Count == 0)
        {
            string warning = $"Lane '{lane.name}' is an ORPHANED lane (no 'Previous Lanes'). Is this a starting lane?";
            warnings.Add(new KeyValuePair<string, TrafficLane>(warning, lane));
            Debug.LogWarning(warning, lane);
        }
    }

    private Vector3 GetSnappedPoint(Vector3 currentPos)
    {
        int layerMask = LayerMask.GetMask("Ground");
        Vector3 rayOrigin = new Vector3(currentPos.x, currentPos.y + 50f, currentPos.z);

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 500f, layerMask))
        {
            return hit.point;
        }

        return new Vector3(currentPos.x, 0, currentPos.z);
    }
}

