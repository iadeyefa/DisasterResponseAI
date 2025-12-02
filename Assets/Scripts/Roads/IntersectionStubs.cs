using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Class for all the entry/exit stubs of a generated intersection.
/// </summary>
public class IntersectionStubs
{
    public List<TrafficLane> InStubs = new List<TrafficLane>();
    public List<TrafficLane> OutStubs = new List<TrafficLane>();
    public Transform Root; 

    public enum StubType { In, Out }

    /// <summary>
    /// Finds the best-matching stubthat points in the given world direction.
    /// Used by the road generator to connect roads to intersections.
    /// </summary>
    public TrafficLane GetBestMatchingStub(Vector3 worldDirection,StubType type)
    {
        var stubsToSearch = (type == StubType.In) ? InStubs : OutStubs;
        if (stubsToSearch == null || stubsToSearch.Count == 0)
        {
            return null;
        }

        TrafficLane bestStub = null;
        float highestDot = -Mathf.Infinity;

        worldDirection.y = 0; 
        worldDirection.Normalize();

        foreach (var stub in stubsToSearch)
        {
            if (stub == null) continue;

            Vector3 stubDir;
            if (type == StubType.In)
            {
                stubDir = stub.GetWorldTangent(0);
            }
            else
            {
                stubDir = stub.GetWorldTangent(1);
            }

            stubDir.y = 0;
            stubDir.Normalize();

            float dot = Vector3.Dot(stubDir, worldDirection);

            if (dot > highestDot)
            {
                highestDot = dot;
                bestStub = stub;
            }
        }

        return bestStub;
    }
}