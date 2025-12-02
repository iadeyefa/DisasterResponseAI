using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Class that contains all logic for generating road pieces
/// </summary>
public static class RoadFactory
{
    /// <summary>
    /// Creates a single TrafficLane instance.
    /// </summary>
    public static TrafficLane CreateLane(Transform parent, TrafficLane lanePrefab,string name, Vector3 p0,Vector3 p1,Vector3 p2, Vector3 p3)
    {
        if (lanePrefab == null) return null;

        TrafficLane lane = (TrafficLane)PrefabUtility.InstantiatePrefab(lanePrefab, parent);
        lane.gameObject.name = name;

        lane.transform.localPosition = Vector3.zero;
        lane.transform.localRotation = Quaternion.identity;

        lane.p0 = p0; 
        lane.p1 = p1; 
        lane.p2 = p2; 
        lane.p3 = p3;

        lane.nextLanes = new List<TrafficLane>();
        lane.previousLanes = new List<TrafficLane>();

        Undo.RegisterCreatedObjectUndo(lane.gameObject, "Create Lane");
        EditorUtility.SetDirty(lane);
        return lane;
    }

    /// <summary>
    /// Connects two lanes
    /// </summary>
    public static void ConnectLanes(TrafficLane from, TrafficLane to)
    {
        if (from == null || to == null) return;

        Undo.RecordObject(from, "Connect Lanes");
        Undo.RecordObject(to, "Connect Lanes");

        if (from.nextLanes == null) from.nextLanes = new List<TrafficLane>();
        if (to.previousLanes == null) to.previousLanes = new List<TrafficLane>();

        if (!from.nextLanes.Contains(to))
        {
            from.nextLanes.Add(to);
        }
        if (!to.previousLanes.Contains(from))
        {
            to.previousLanes.Add(from);
        }

        EditorUtility.SetDirty(from);
        EditorUtility.SetDirty(to);
    }

    /// <summary>
    /// Creates a new lane to bridge two existing lanes.
    /// </summary>
    public static TrafficLane BridgeLanes(TrafficLane fromLane, TrafficLane toLane, Transform parent, TrafficLane lanePrefab)
    {
        if (fromLane == null || toLane == null || lanePrefab == null) return null;

        Vector3 new_p0 = fromLane.GetWorldPoint(1);
        Vector3 new_p3 = toLane.GetWorldPoint(0);
        Vector3 tangent_p0 = fromLane.GetWorldTangent(1).normalized;
        Vector3 tangent_p3 = toLane.GetWorldTangent(0).normalized;

        //Flatten Y coordinates
        new_p0.y = 0;
        new_p3.y = 0;
        tangent_p0.y = 0;
        tangent_p3.y = 0;
        tangent_p0.Normalize();
        tangent_p3.Normalize();

        float controlPointLength = Vector3.Distance(new_p0, new_p3) / 3.0f;
        Vector3 new_p1 = new_p0 + tangent_p0 * controlPointLength;
        Vector3 new_p2 = new_p3 - tangent_p3 * controlPointLength;

        //Convert back to local space of the parent
        Transform p = parent ?? fromLane.transform.parent;

        Vector3 local_p0 = (p != null) ? p.InverseTransformPoint(new_p0) : new_p0;
        Vector3 local_p1 = (p != null) ? p.InverseTransformPoint(new_p1) : new_p1;
        Vector3 local_p2 = (p != null) ? p.InverseTransformPoint(new_p2) : new_p2;
        Vector3 local_p3 = (p != null) ? p.InverseTransformPoint(new_p3) : new_p3;

        TrafficLane newLane = CreateLane(p, lanePrefab, $"Bridge_{fromLane.name}_to_{toLane.name}",
            local_p0, local_p1, local_p2, local_p3);

        if (newLane != null)
        {
            ConnectLanes(fromLane, newLane);
            ConnectLanes(newLane, toLane);

            newLane.transform.localPosition = Vector3.zero;
            newLane.transform.localRotation = Quaternion.identity;
        }
        return newLane;
    }

    /// <summary>
    /// Creates a 4-Way intersection
    /// </summary>
    public static IntersectionStubs CreateFourWayIntersection(Transform intRoot, TrafficLane lanePrefab, float intersectionSize, float laneWidth, float stubLength)
    {
        float s = intersectionSize / 2.0f; float w = laneWidth / 2.0f; float l = stubLength;

        //Define all 8 ports
        Vector3 p_S_In_start = new Vector3(-w,0,-s -l); Vector3 p_S_In_end = new Vector3(-w, 0,-s);
        Vector3 p_S_Out_start = new Vector3(w, 0,-s); Vector3 p_S_Out_end = new Vector3(w, 0, -s - l);
        Vector3 p_N_In_start = new Vector3(w,0,s + l); Vector3 p_N_In_end = new Vector3(w, 0, s);
        Vector3 p_N_Out_start = new Vector3(-w, 0, s); Vector3 p_N_Out_end = new Vector3(-w, 0, s + l);
        Vector3 p_W_In_start = new Vector3(-s - l, 0, w); Vector3 p_W_In_end = new Vector3(-s, 0, w);
        Vector3 p_W_Out_start = new Vector3(-s, 0, -w); Vector3 p_W_Out_end = new Vector3(-s - l, 0, -w);
        Vector3 p_E_In_start = new Vector3(s + l,0, -w); Vector3 p_E_In_end = new Vector3(s, 0, -w);
        Vector3 p_E_Out_start = new Vector3(s, 0, w); Vector3 p_E_Out_end = new Vector3(s + l,0, w);






        //Create all 8 stub lanes
        TrafficLane stub_S_In = CreateLane(intRoot, lanePrefab, "Stub_S_In", p_S_In_start, p_S_In_start + Vector3.forward * l * 0.5f, p_S_In_end - Vector3.forward * l * 0.5f, p_S_In_end);
        TrafficLane stub_S_Out = CreateLane(intRoot, lanePrefab, "Stub_S_Out", p_S_Out_start, p_S_Out_start + Vector3.back * l * 0.5f, p_S_Out_end - Vector3.back * l * 0.5f, p_S_Out_end);
        TrafficLane stub_N_In = CreateLane(intRoot, lanePrefab, "Stub_N_In", p_N_In_start, p_N_In_start + Vector3.back * l * 0.5f, p_N_In_end - Vector3.back * l * 0.5f, p_N_In_end);
        TrafficLane stub_N_Out = CreateLane(intRoot, lanePrefab, "Stub_N_Out", p_N_Out_start, p_N_Out_start + Vector3.forward * l * 0.5f, p_N_Out_end - Vector3.forward * l * 0.5f, p_N_Out_end);
      
        TrafficLane stub_W_In = CreateLane(intRoot, lanePrefab, "Stub_W_In", p_W_In_start, p_W_In_start + Vector3.right * l * 0.5f, p_W_In_end - Vector3.right * l * 0.5f, p_W_In_end);
        TrafficLane stub_W_Out = CreateLane(intRoot, lanePrefab, "Stub_W_Out", p_W_Out_start, p_W_Out_start + Vector3.left * l * 0.5f, p_W_Out_end - Vector3.left * l * 0.5f, p_W_Out_end);
        TrafficLane stub_E_In = CreateLane(intRoot, lanePrefab, "Stub_E_In", p_E_In_start, p_E_In_start + Vector3.left * l * 0.5f, p_E_In_end - Vector3.left * l * 0.5f, p_E_In_end);
        TrafficLane stub_E_Out = CreateLane(intRoot, lanePrefab, "Stub_E_Out", p_E_Out_start, p_E_Out_start + Vector3.right * l * 0.5f, p_E_Out_end - Vector3.right * l * 0.5f, p_E_Out_end);

        //From South
        TrafficLane s_n = CreateLane(intRoot, lanePrefab, "Lane_S_to_N", p_S_In_end, new Vector3(-w, 0, 0), new Vector3(-w, 0, 0), p_N_Out_start);
        TrafficLane s_w = CreateLane(intRoot, lanePrefab, "Lane_S_to_W", p_S_In_end, new Vector3(p_S_In_end.x, 0, p_W_Out_start.z), new Vector3(p_S_In_end.x, 0, p_W_Out_start.z), p_W_Out_start);
        TrafficLane s_e = CreateLane(intRoot, lanePrefab, "Lane_S_to_E", p_S_In_end, new Vector3(-w, 0, -w), new Vector3(w, 0, w), p_E_Out_start);
        ConnectLanes(stub_S_In, s_n); ConnectLanes(stub_S_In, s_w); ConnectLanes(stub_S_In, s_e);
        ConnectLanes(s_n, stub_N_Out); ConnectLanes(s_w, stub_W_Out); ConnectLanes(s_e, stub_E_Out);





        //From North
        TrafficLane n_s = CreateLane(intRoot, lanePrefab, "Lane_N_to_S", p_N_In_end, new Vector3(w, 0, 0), new Vector3(w, 0, 0), p_S_Out_start);
        TrafficLane n_e = CreateLane(intRoot, lanePrefab, "Lane_N_to_E", p_N_In_end, new Vector3(p_N_In_end.x, 0, p_E_Out_start.z), new Vector3(p_N_In_end.x, 0, p_E_Out_start.z), p_E_Out_start);
        TrafficLane n_w = CreateLane(intRoot, lanePrefab, "Lane_N_to_W", p_N_In_end, new Vector3(w, 0, w), new Vector3(-w, 0, -w), p_W_Out_start);
        ConnectLanes(stub_N_In, n_s); ConnectLanes(stub_N_In, n_e); ConnectLanes(stub_N_In, n_w);
        ConnectLanes(n_s, stub_S_Out); ConnectLanes(n_e, stub_E_Out); ConnectLanes(n_w, stub_W_Out);

        //From West
        TrafficLane w_e = CreateLane(intRoot, lanePrefab, "Lane_W_to_E", p_W_In_end, new Vector3(0,0, w), new Vector3(0,0, w), p_E_Out_start);
        TrafficLane w_s = CreateLane(intRoot, lanePrefab, "Lane_W_to_S", p_W_In_end, new Vector3(w, 0, w), new Vector3(-w, 0, -w), p_S_Out_start);
        TrafficLane w_n = CreateLane(intRoot, lanePrefab, "Lane_W_to_N", p_W_In_end, new Vector3(-w,0, w), new Vector3(-w,0, s), p_N_Out_start);
        ConnectLanes(stub_W_In, w_e); ConnectLanes(stub_W_In, w_s); ConnectLanes(stub_W_In, w_n);
        ConnectLanes(w_e, stub_E_Out); ConnectLanes(w_s, stub_S_Out); ConnectLanes(w_n, stub_N_Out);






        //From East
        TrafficLane e_w = CreateLane(intRoot, lanePrefab, "Lane_E_to_W", p_E_In_end, new Vector3(0,0, -w), new Vector3(0,0, -w), p_W_Out_start);
        TrafficLane e_n = CreateLane(intRoot, lanePrefab, "Lane_E_to_N", p_E_In_end, new Vector3(w, 0, -w), new Vector3(w, 0, w), p_N_Out_start);
        TrafficLane e_s = CreateLane(intRoot, lanePrefab, "Lane_E_to_S", p_E_In_end, new Vector3(p_S_Out_start.x, 0, p_E_In_end.z), new Vector3(p_S_Out_start.x, 0, p_E_In_end.z), p_S_Out_start);
        ConnectLanes(stub_E_In, e_w); ConnectLanes(stub_E_In, e_n); ConnectLanes(stub_E_In, e_s);
        ConnectLanes(e_w, stub_W_Out); ConnectLanes(e_n, stub_N_Out); ConnectLanes(e_s, stub_S_Out);

        IntersectionStubs stubs = new IntersectionStubs { Root = intRoot };
        stubs.InStubs.AddRange(new[] { stub_S_In, stub_N_In, stub_W_In, stub_E_In });
        stubs.OutStubs.AddRange(new[] { stub_S_Out, stub_N_Out, stub_W_Out, stub_E_Out });
        return stubs;
    }


}