using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;


/// <summary>
/// Traffic Intersection that can auto-generate lanes and traffic lights based on connectors
/// </summary>
public class TrafficIntersection : MonoBehaviour
{
    [Header("Intersection Settings")]
    public TrafficLane lanePrefab;
    public GameObject trafficLightPrefab;

    private float intersectionSize = 25.0f;
    private float stubLength = 10.0f;

    [Header("Connectors")]
    public List<IntersectionConnector> connectors = new List<IntersectionConnector>();

    public float GetIntersectionSize() { return intersectionSize; }
    public float GetStubLength() { return stubLength; }


    /// <summary>
    /// Rebuild the intersection lanes and traffic lights
    /// </summary>
    [ContextMenu("Rebuild Intersection")]
    public void RebuildIntersection()
    {
        if (lanePrefab == null)
        {
            Debug.LogError("Lane Prefab is not assigned", this);
            return;
        }

        //Cleanup existing lanes
        var existingLanes = GetComponentsInChildren<TrafficLane>();
        foreach (var lane in existingLanes)
        {
            if (Application.isPlaying) Destroy(lane.gameObject);
            else DestroyImmediate(lane.gameObject);
        }

        //clean up existing traffic lights & groups
        TrafficLightGroup lightGroupComp = GetComponent<TrafficLightGroup>();
        if (lightGroupComp != null)
        {
            for (int i = 0; i < lightGroupComp.lightGroups.Count; i++)
            {
                foreach (var light in lightGroupComp.lightGroups[i].onTrafficLights)
                {
                    if (light != null)
                    {
                        if (Application.isPlaying) Destroy(light.gameObject);
                        else DestroyImmediate(light.gameObject);
                    }
                }
            }


            if (Application.isPlaying) Destroy(lightGroupComp);
            else DestroyImmediate(lightGroupComp);
        }

        //check if we need traffic lights 
        bool useTrafficLights = connectors.Count > 2;

        if (useTrafficLights)
        {
            lightGroupComp = gameObject.AddComponent<TrafficLightGroup>();
            lightGroupComp.lightGroups = new List<TrafficLightGroup.LaneGroup>();
        }

        foreach (var connector in connectors)
        {
            connector.incomingStubs.Clear();
            connector.outgoingStubs.Clear();
        }

        Dictionary<string, TrafficLightGroup.LaneGroup> groupMap = new Dictionary<string, TrafficLightGroup.LaneGroup>();

        //create stubs and light groups
        foreach (var connector in connectors)
        {
            CreateStubsAndLightGroup(connector, groupMap, useTrafficLights);
        }

        //create turning lanes
        foreach (var fromConnector in connectors)
        {
            foreach (var toConnector in connectors)
            {
                if (fromConnector == toConnector) continue;

                foreach (var inLane in fromConnector.incomingStubs)
                {
                    foreach (var outLane in toConnector.outgoingStubs)
                    {
                        TrafficLane turnLane = CreateTurningLane(inLane, outLane);

                        if (useTrafficLights && groupMap.ContainsKey(fromConnector.name))
                        {
                            groupMap[fromConnector.name].lanes.Add(turnLane);
                        }

#if UNITY_EDITOR
                        RoadFactory.ConnectLanes(inLane, turnLane);
                        RoadFactory.ConnectLanes(turnLane, outLane);
#endif
                    }
                }
            }
        }

        //if traffic lights, finalize groups
        if (useTrafficLights && lightGroupComp != null)
        {
            foreach (var group in groupMap.Values)
            {
                lightGroupComp.lightGroups.Add(group);
            }
        }
    }
    /// <summary>
    /// Create stubs for a connector and setup traffic light group if needed
    /// </summary>
    private void CreateStubsAndLightGroup(IntersectionConnector connector, Dictionary<string, TrafficLightGroup.LaneGroup> groupMap, bool useTrafficLights)
    {
        float s = intersectionSize / 2.0f;
        float l = stubLength;
        float w = connector.laneWidth;

        Quaternion rot = Quaternion.Euler(0, connector.rotation, 0);
        Vector3 localForward = rot * Vector3.forward;
        Vector3 localRight = rot * Vector3.right;

        //outgoing stubs
        for (int i = 0; i < connector.outLanes; i++)
        {
            float offset = (i * w) + (w / 2f);
            Vector3 sideOffset = -localRight * offset;
            Vector3 p_Start = (localForward * s) + sideOffset;
            Vector3 p_End = (localForward * (s + l)) + sideOffset;
            TrafficLane stub = CreateLane($"Stub_{connector.name}_Out_{i}", p_Start, p_End);
            connector.outgoingStubs.Add(stub);
        }

        //incomiung stubs
        List<TrafficLane> newIncomingLanes = new List<TrafficLane>();
        for (int i = 0; i < connector.inLanes; i++)
        {
            float offset = (i * w) + (w / 2f);
            Vector3 sideOffset = localRight * offset;
            Vector3 p_Start = (localForward * (s + l)) + sideOffset;
            Vector3 p_End = (localForward * s) + sideOffset;
            TrafficLane stub = CreateLane($"Stub_{connector.name}_In_{i}", p_Start, p_End);
            connector.incomingStubs.Add(stub);
            newIncomingLanes.Add(stub);
        }

        //traffic light group
        if (useTrafficLights && newIncomingLanes.Count > 0)
        {
            TrafficLightGroup.LaneGroup newLaneGroupData = new TrafficLightGroup.LaneGroup();
            newLaneGroupData.groupName = connector.name;
            newLaneGroupData.lanes = new List<TrafficLane>();

            TrafficLight spawnedLight = SpawnTrafficLightForGroup(newIncomingLanes, connector.name);

            if (spawnedLight != null)
            {
                newLaneGroupData.onTrafficLights = new TrafficLight[] { spawnedLight };
            }
            else
            {
                newLaneGroupData.onTrafficLights = new TrafficLight[0];
            }

            if (!groupMap.ContainsKey(connector.name))
            {
                groupMap.Add(connector.name, newLaneGroupData);
            }
        }
    }
    /// <summary>
    /// Create and position a traffic light for a group of lanes
    /// </summary>
    private TrafficLight SpawnTrafficLightForGroup(List<TrafficLane> lanes, string connectorName)
    {
        if (trafficLightPrefab == null || lanes.Count == 0) return null;

        GameObject lightObj = null;

#if UNITY_EDITOR
        lightObj = (GameObject)PrefabUtility.InstantiatePrefab(trafficLightPrefab);
#else
        lightObj = Instantiate(trafficLightPrefab);
#endif

        if (lightObj == null) return null;


        lightObj.transform.localScale = new Vector3(-1, 1, 1);

        lightObj.transform.SetParent(this.transform, false);
        lightObj.name = $"TrafficLight_{connectorName}";

        Vector3 averagePos = Vector3.zero;
        foreach (var lane in lanes) averagePos += lane.GetWorldPoint(1.0f);
        averagePos /= lanes.Count;

        Vector3 worldP0 = lanes[0].transform.TransformPoint(lanes[0].p0);
        averagePos = new Vector3(averagePos.x, worldP0.y, averagePos.z);  

        lightObj.transform.position = averagePos;
        

        Vector3 flowDirection = lanes[0].GetWorldTangent(1.0f);
        if (flowDirection != Vector3.zero)
        {
            lightObj.transform.rotation = Quaternion.LookRotation(-flowDirection);
        }

        lightObj.transform.Translate(new Vector3(2, -1, 2), Space.Self);

        return lightObj.GetComponent<TrafficLight>();
    }
    /// <summary>
    /// Create a lane from local start to local end
    /// </summary>
    private TrafficLane CreateLane(string name, Vector3 localStart, Vector3 localEnd)
    {
        Vector3 p0 = localStart;
        Vector3 p3 = localEnd;
        Vector3 dir = (p3 - p0);
        Vector3 p1 = p0 + dir * 0.33f;
        Vector3 p2 = p0 + dir * 0.66f;

#if UNITY_EDITOR
        TrafficLane lane = RoadFactory.CreateLane(this.transform, lanePrefab, name, p0, p1, p2, p3);
        return lane;
#else
        return null; 
#endif
    }
    /// <summary>
    /// Create a turning lane between two stubs
    /// </summary>
    private TrafficLane CreateTurningLane(TrafficLane inStub, TrafficLane outStub)
    {
        Vector3 p0 = inStub.p3;
        Vector3 p3 = outStub.p0;
        Vector3 tan0 = (inStub.p3 - inStub.p2).normalized;
        Vector3 tan3 = (outStub.p1 - outStub.p0).normalized;

        float dist = Vector3.Distance(p0, p3) / 3.0f;
        Vector3 p1 = p0 + tan0 * dist;
        Vector3 p2 = p3 - tan3 * dist;

#if UNITY_EDITOR
        TrafficLane lane = RoadFactory.CreateLane(this.transform, lanePrefab,
            $"Turn_{inStub.name}_to_{outStub.name}", p0, p1, p2, p3);
        return lane;
#else
        return null;
#endif
    }


    /// <summary>
    /// Set intersection lanes
    /// </summary>
    public void Start()
    {
        TrafficLane[] lanes = transform.gameObject.GetComponentsInChildren<TrafficLane>();
        for (int i = 0; i < lanes.Length; i++)
        {
            if (lanes[i].name.StartsWith("Turn_"))
            {
                lanes[i].isIntersectionLane = true;
            }
        }
    }
}