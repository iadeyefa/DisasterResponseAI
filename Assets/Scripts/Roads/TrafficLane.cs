using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// A single lane in the traffic system.
/// </summary>
public class TrafficLane : MonoBehaviour
{
    [Header("Lane Settings")]
    public float speedLimit = 10.0f;
    [Header("Path - Bezier points")]
    public Vector3 p0;
    public Vector3 p1 = Vector3.forward * 5;
    public Vector3 p2 = Vector3.forward * 10;
    public Vector3 p3 = Vector3.forward * 15;
    [Header("Connections")]
    public List<TrafficLane> nextLanes = new List<TrafficLane>();
    public TrafficLane neighborLaneLeft = null;
    public TrafficLane neighborLaneRight = null;
    public List<TrafficLane> previousLanes = new List<TrafficLane>();
    [Header("State")]
    public bool isStopSignalActive = false;
    public bool isClosed = false;

    [Header("Runtime Visualization")]
    public bool showRuntimeVisuals = true;
    private float lineWidth = 2.0f;
    public Material lineMaterial; 

    private LineRenderer lr;
    private int segments = 20; 



    [Header("Settings")]

    //keys from editor prefs
    public const string GIZMO_SHOW_PATH_KEY = "RoadSystem_ShowBezierPaths";
    public const string GIZMO_SHOW_NEXT_KEY = "RoadSystem_ShowNextLaneConnections";
    public const string GIZMO_SHOW_NEIGHBOR_KEY = "RoadSystem_ShowNeighborLanes";
    public const string GIZMO_SHOW_SPHERES_KEY = "RoadSystem_ShowStartEndSpheres";
    public const string GIZMO_SHOW_ARROWS_KEY = "RoadSystem_ShowDirectionArrows";
    public const string GIZMO_SHOW_TEXT_KEY = "RoadSystem_ShowSpeedLimitText";
    public const string GIZMO_SIMPLE_LINES_KEY = "RoadSystem_UseSimpleLines";
    public const string GIZMO_CULLING_DISTANCE_KEY = "RoadSystem_GizmoCullDistance";
    public const string GIZMO_ONLY_SELECTED_KEY = "RoadSystem_OnlyShowSelectedContext";

    //for tracking
    public bool isIntersectionLane = false;
    public List<GameObject> carsInLane = new List<GameObject>();

#if UNITY_EDITOR
    private static HashSet<TrafficLane> _relatedLanesCache = new HashSet<TrafficLane>();
    private static TrafficLane _lastSelectedLane = null;
#endif

    //private void Start()
    //{
    //    p0 = new Vector3(p0.x, 0, p0.z);
    //    p1 = new Vector3(p1.x, 0, p1.z);
    //    p2 = new Vector3(p2.x, 0, p2.z);
    //    p3 = new Vector3(p3.x, 0, p3.z);
    //}
    /// <summary>
    /// Gets cars in the current lane
    /// </summary>
    public int GetCarCount()
    {
        return carsInLane.Count;
    }
    /// <summary>
    /// Retuns a vector3 point on the bezier curve at t (0-1)
    /// </summary>
    private Vector3 GetPoint(float t)
    {
        t = Mathf.Clamp01(t);
        float oneMinusT = 1f - t;
        return
            oneMinusT * oneMinusT * oneMinusT * p0 + 3f * oneMinusT * oneMinusT * t * p1 + 3f * oneMinusT * t * t * p2 + t * t * t * p3;
    }
    /// <summary>
    /// Returns the tangent vector on the bezier curve at t (0-1)
    /// </summary>
    private Vector3 GetTangent(float t)
    {
        t = Mathf.Clamp01(t);
        float oneMinusT = 1f - t;
        return
            3f * oneMinusT * oneMinusT * (p1 - p0) + 6f * oneMinusT * t * (p2 - p1) + 3f * t * t * (p3 - p2);
    }
    /// <summary>
    /// Returns a world-space point on the bezier curve at t (0-1)
    /// </summary>
    public Vector3 GetWorldPoint(float t)
    {
        return transform.TransformPoint(GetPoint(t));
    }
    /// <summary>
    /// Returns a world-space tangent vector on the bezier curve at t (0-1)
    /// </summary>
    public Vector3 GetWorldTangent(float t)
    {
        return transform.TransformDirection(GetTangent(t));
    }


#if UNITY_EDITOR
    /// <summary>
    /// Draw the lane gizmos in the editor
    /// </summary>
    private void OnDrawGizmos()
    {
        //Only Show Selected Culling Logic
        bool onlyShowSelected = EditorPrefs.GetBool(GIZMO_ONLY_SELECTED_KEY, false);

        if (onlyShowSelected)
        {
            var selection = Selection.activeGameObject;
            TrafficLane selectedLane = (selection != null) ? selection.GetComponent<TrafficLane>() : null;

            if (selectedLane == null)
            {
                return; 
            }

            //A lane is selected. Check cache
            if (selectedLane != _lastSelectedLane)
            {
                //Selection has changed. Rebuild cache
                _relatedLanesCache.Clear();
                _relatedLanesCache.Add(selectedLane);
                if (selectedLane.nextLanes != null)
                {
                    foreach (var lane in selectedLane.nextLanes) { if (lane != null) _relatedLanesCache.Add(lane); }
                }
                if (selectedLane.previousLanes != null)
                {
                    foreach (var lane in selectedLane.previousLanes) { if (lane != null) _relatedLanesCache.Add(lane); }
                }
                _lastSelectedLane = selectedLane;
            }

            if (!_relatedLanesCache.Contains(this))
            {
                return; 
            }
        }


        //Distance Culling Logic
        float maxDist = EditorPrefs.GetFloat(GIZMO_CULLING_DISTANCE_KEY, 1000f);

        if (maxDist > 0 && Selection.activeGameObject != this.gameObject) 
        {
            Camera sceneCamera = SceneView.currentDrawingSceneView.camera;
            if (sceneCamera != null)
            {
                Vector3 worldCenter = GetWorldPoint(0.5f);
                float distanceSqr = (sceneCamera.transform.position - worldCenter).sqrMagnitude;
                if (distanceSqr > (maxDist * maxDist))
                {
                    return; 
                }
            }
        }

        //lane connections
        if (EditorPrefs.GetBool(GIZMO_SHOW_NEXT_KEY, true))
        {
            Gizmos.color = Color.green;
            Vector3 worldP3_self = transform.TransformPoint(p3);
            if (nextLanes != null)
            {
                foreach (var nextLane in nextLanes)
                {
                    if (nextLane != null)
                    {
                        Vector3 nextWorldP0 = nextLane.transform.TransformPoint(nextLane.p0);
                        Gizmos.DrawLine(worldP3_self, nextWorldP0);
                        Vector3 dir = (nextWorldP0 - worldP3_self).normalized;
                        if (dir != Vector3.zero)
                        {
                            Vector3 right = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 30, 0) * Vector3.back;
                            Vector3 left = Quaternion.LookRotation(dir) * Quaternion.Euler(0, -30, 0) * Vector3.back;
                            Gizmos.DrawLine(nextWorldP0, nextWorldP0 + right * 0.5f);
                            Gizmos.DrawLine(nextWorldP0, nextWorldP0 + left * 0.5f);
                        }
                    }
                }
            }
        }

        //neighbor lanes
        if (EditorPrefs.GetBool(GIZMO_SHOW_NEIGHBOR_KEY, true))
        {
            Handles.color = Color.cyan;
            Vector3 worldCenter_self = GetWorldPoint(0.5f);
            if (neighborLaneLeft != null)
            {
                Vector3 worldCenter_left = neighborLaneLeft.GetWorldPoint(0.5f);
                Handles.DrawDottedLine(worldCenter_self, worldCenter_left, 5.0f);
            }
            if (neighborLaneRight != null)
            {
                Vector3 worldCenter_right = neighborLaneRight.GetWorldPoint(0.5f);
                Handles.DrawDottedLine(worldCenter_self, worldCenter_right, 5.0f);
            }
        }

        //start end spheres
        Vector3 worldP0 = GetWorldPoint(0);
        Vector3 worldP3 = GetWorldPoint(1);

        if (EditorPrefs.GetBool(GIZMO_SHOW_SPHERES_KEY, true))
        {
            if (isStopSignalActive || isClosed) Gizmos.color = new Color(1, 0, 0, 1f);
            else Gizmos.color = new Color(0, 1, 0, 0.7f);
            Gizmos.DrawSphere(worldP0, 0.25f);

            Gizmos.color = new Color(1, 0, 0, 0.7f);
            Gizmos.DrawSphere(worldP3, 0.25f);
        }

        //arrows
        if (EditorPrefs.GetBool(GIZMO_SHOW_ARROWS_KEY, true))
        {
            Handles.color = new Color(1, 1, 1, 0.7f);
            int numArrows = 3;
            float arrowSize = 0.75f;
            for (int i = 1; i <= numArrows; i++)
            {
                float t = (float)i / (numArrows + 1);
                Vector3 point = GetWorldPoint(t);
                Vector3 tangent = GetWorldTangent(t).normalized;
                if (tangent != Vector3.zero)
                {
                    Handles.ArrowHandleCap(0, point, Quaternion.LookRotation(tangent), arrowSize, EventType.Repaint);
                }
            }
        }

        //text
        if (EditorPrefs.GetBool(GIZMO_SHOW_TEXT_KEY, true))
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleCenter;
            style.fontStyle = FontStyle.Bold;
            Vector3 textPos = worldP0 + Vector3.up * 0.5f;
            string text = $"Speed: {speedLimit}";
            Handles.Label(textPos, text, style);
        }

        //path drawing logic
        Vector3 worldP1 = transform.TransformPoint(p1);
        Vector3 worldP2 = transform.TransformPoint(p2);

        Color pathColor = new Color(1, 1, 1, 0.5f);
        if (isClosed) pathColor = new Color(1, 0, 0, 0.5f);
        else if (isStopSignalActive) pathColor = new Color(1, 0.8f, 0, 0.5f);
        Handles.color = pathColor;

        if (Selection.activeGameObject == this.gameObject)
        {
            //if selected draw the high-detail Bézier curve
            Handles.DrawBezier(worldP0, worldP3, worldP1, worldP2, Handles.color, null, 2f);
        }
        else
        {
            //if not selected, draw basic line
            if (EditorPrefs.GetBool(GIZMO_SHOW_PATH_KEY, true))
            {
                if (EditorPrefs.GetBool(GIZMO_SIMPLE_LINES_KEY, false))
                {
                    Handles.DrawLine(worldP0, worldP3, 2f);
                }
                else
                {
                    Handles.DrawBezier(worldP0, worldP3, worldP1, worldP2, Handles.color, null, 2f);
                }
            }
        }
    }
#endif





    private void Start()
    {
        if (showRuntimeVisuals)
        {
            InitializeLineRenderer();
            DrawStaticCurve(); 
        }

        SetSpeedLimitBasedOnRoadLength();
    }
    /// <summary>
    /// Calculates the approximate length of the road by sampling points along the Bezier curve.
    /// </summary>
    private float CalculateRoadLength()
    {
        float length = 0f;
        int numSegments = 100; 

        Vector3 previousPoint = GetWorldPoint(0);
        for (int i = 1; i <= numSegments; i++)
        {
            float t = (float)i / numSegments;
            Vector3 currentPoint = GetWorldPoint(t);
            length += Vector3.Distance(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }

        return length;
    }
    /// <summary>
    /// Sets the speed limit based on the calculated road length.
    /// </summary>
    private void SetSpeedLimitBasedOnRoadLength()
    {
        float roadLength = CalculateRoadLength();

        if (roadLength < 30f)
        {
            speedLimit = 20f;  
        }
        else if (roadLength < 60f)
        {
            speedLimit = 40f;  
        }
        else
        {
            speedLimit = 60f; 
        }
    }
    private float timer = 0f;
    /// <summary>
    /// Updates the traffic color 
    /// </summary>
    private void Update()
    {
        if (!showRuntimeVisuals) return;


        if(TrafficStatsManager.Instance.overlayToggle.isOn == false)
        {
            lr.enabled = false;
            return;
        }
        else
        {
            lr.enabled = true;
        }

        timer += Time.deltaTime;
        if (timer > 5f) // Only update colors twice per second
        {
            
            UpdateTrafficColor();
            timer = 0f;
        }


    }
    /// <summary>
    /// Init line renderer for runtime visualization
    /// </summary>
    private void InitializeLineRenderer()
    {
        lr = GetComponent<LineRenderer>();
        if (lr == null) lr = gameObject.AddComponent<LineRenderer>();

        lr.useWorldSpace = true;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;

        if (lineMaterial != null)
        {
            lr.material = lineMaterial;
        }
        else
        {
            lr.material = new Material(Shader.Find("Sprites/Default"));
        }
    }

    /// <summary>
    /// Calculates the Bezier points and sends them to the LineRenderer
    /// </summary>
    public void DrawStaticCurve()
    {
        if (lr == null) return;

        lr.positionCount = segments + 1;
        Vector3[] points = new Vector3[segments + 1];

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            points[i] = GetWorldPoint(t);
            points[i].y += 0.1f;
        }

        lr.SetPositions(points);
    }

    /// <summary>
    /// Changes the line color based on the number of cars.
    /// </summary>
    private void UpdateTrafficColor()
    {
        Color targetColor = Color.green; 

        int count = GetCarCount();

        if (count >= 5)
        {
            targetColor = Color.red; 
        }
        else if (count >= 2)
        {
            targetColor = Color.yellow;
        }

        lr.startColor = targetColor;
        lr.endColor = targetColor;
    }


}