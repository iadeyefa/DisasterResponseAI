using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Single road connecting to a TrafficIntersection.
/// </summary>
[System.Serializable]
public class IntersectionConnector
{
    public string name;

    [Header("Road Properties")]
    [Range(0, 360)]
    public float rotation = 0f;

    public float laneWidth = 3.5f;
    public int inLanes = 1;
    public int outLanes = 1;

    [Header("Generated Stubs")]
    public List<TrafficLane> outgoingStubs = new List<TrafficLane>();
    public List<TrafficLane> incomingStubs = new List<TrafficLane>();
}