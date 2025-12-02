using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Road segment that connects two TrafficIntersection connectors
/// </summary>
public class RoadSegment : MonoBehaviour
{
    public TrafficIntersection intersectionA;
    public string connectorAName; 

    public TrafficIntersection intersectionB;
    public string connectorBName; 

    public TrafficLane lanePrefab;

    /// <summary>
    /// Rebuilds the entire road segment
    /// </summary>
    [ContextMenu("Rebuild Road Segment")]
    public void RebuildRoad()
    {
#if UNITY_EDITOR
        if (intersectionA == null || intersectionB == null)
        {
            return;
        }
        if (lanePrefab == null)
        {
            return;
        }
        if (string.IsNullOrEmpty(connectorAName) || string.IsNullOrEmpty(connectorBName))
        {
            return;
        }

        var connectorA = intersectionA.connectors.FirstOrDefault(c => c.name == connectorAName);
        var connectorB = intersectionB.connectors.FirstOrDefault(c => c.name == connectorBName);

        if (connectorA == null || connectorB == null)
        {
            return;
        }


        var existingLanes = GetComponentsInChildren<TrafficLane>();
        foreach (var lane in existingLanes)
        {
            if (Application.isPlaying)
                Destroy(lane.gameObject);
            else
                UnityEditor.Undo.DestroyObjectImmediate(lane.gameObject);
        }

        ConnectStubs(connectorA.outgoingStubs, connectorB.incomingStubs, "A_to_B");
        ConnectStubs(connectorB.outgoingStubs, connectorA.incomingStubs, "B_to_A");
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// Handles the connection logic and lane mismatch warnings.
    /// </summary>
    private void ConnectStubs(List<TrafficLane> fromStubs, List<TrafficLane> toStubs, string direction)
    {
        if (fromStubs.Count == 0 || toStubs.Count == 0)
        {
            Debug.LogWarning($"Cannot connect {direction}", this);
            return;
        }

        int lanesToConnect = Mathf.Min(fromStubs.Count, toStubs.Count);

        if (fromStubs.Count != toStubs.Count)
        {
            Debug.LogWarning($"Lane mismatch connecting {direction}", this);
        }

        for (int i = 0; i < lanesToConnect; i++)
        {
            TrafficLane fromStub = fromStubs[i];
            TrafficLane toStub = toStubs[i];

            if (fromStub == null || toStub == null)
            {
                Debug.LogError($"Cannot connect {direction} lane {i}: A stub is null.", this);
                continue;
            }

            RoadFactory.BridgeLanes(fromStub, toStub, this.transform, lanePrefab);
        }
    }
#endif

    /// <summary>
    /// Automatically finds and sets the two connectors that
    /// are most closely aligned between the intersections.
    /// </summary>
    public void AutoDetectConnectors()
    {
        if (intersectionA == null || intersectionB == null)
        {
            Debug.LogError("Cannot auto-detect", this);
            return;
        }

        Vector3 posA = intersectionA.transform.position;
        Vector3 posB = intersectionB.transform.position;

        //Calculate the 2D direction vectors between intersections
        Vector3 dir_A_to_B = (posB - posA);
        dir_A_to_B.y = 0;
        dir_A_to_B.Normalize();

        Vector3 dir_B_to_A = (posA - posB);
        dir_B_to_A.y = 0;
        dir_B_to_A.Normalize();

        IntersectionConnector bestConnectorA = null;
        float maxDotA = -Mathf.Infinity;

        foreach (var connector in intersectionA.connectors)
        {
            Vector3 connWorldDir = intersectionA.transform.rotation * Quaternion.Euler(0, connector.rotation, 0) * Vector3.forward;
            connWorldDir.y = 0;
            connWorldDir.Normalize();

            float dot = Vector3.Dot(connWorldDir, dir_A_to_B);
            if (dot > maxDotA)
            {
                maxDotA = dot;
                bestConnectorA = connector;
            }
        }

        IntersectionConnector bestConnectorB = null;
        float maxDotB = -Mathf.Infinity;

        foreach (var connector in intersectionB.connectors)
        {
            Vector3 connWorldDir = intersectionB.transform.rotation * Quaternion.Euler(0, connector.rotation, 0) * Vector3.forward;
            connWorldDir.y = 0;
            connWorldDir.Normalize();

            float dot = Vector3.Dot(connWorldDir, dir_B_to_A);
            if (dot > maxDotB)
            {
                maxDotB = dot;
                bestConnectorB = connector;
            }
        }

        if (bestConnectorA != null && bestConnectorB != null)
        {
            this.connectorAName = bestConnectorA.name;
            this.connectorBName = bestConnectorB.name;
        }
        else
        {
            Debug.LogError("Auto-Detect failed", this);
        }
    }
}