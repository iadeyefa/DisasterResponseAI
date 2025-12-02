using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Vehicle Script
/// Handles main navigation (A*) and local avoidance
/// </summary>
public class TrafficVehicle : MonoBehaviour
{
    public CarTrafficTracker tracker;
    public GameObject[] cars;
    [Header("Navigation")]
    public TrafficLane currentLane;
    public TrafficLane targetLane;

    [Header("Movement")]
    public float maxSpeed = 15.0f;
    public float rotationSpeed = 5.0f;

    [Header("Local Avoidance")]
    public float detectionDistance = 10.0f;
    public float stoppingDistance = 3.0f;
    public LayerMask vehicleLayerMask;

    [Header("Lane Changing")]
    public bool enableLaneChanging = true;
    public float laneChangeSpeed = 2.0f;
    public float laneChangeCooldown = 1.0f;
    public float laneChangeCheckDistance = 15.0f;
    public float laneChangeTriggerProximity = 0.5f;

    //public float totalAccumulatedWaitTime = 0.0f;

    ///The VehicleSpawner listens to this to assign a new target.
    public System.Action<TrafficVehicle> OnTargetReached;


    //Progress along the Bezier curve 0 to 1
    private float t = 0.0f;

    // The calculated path to the target.
    private List<TrafficLane> path = new List<TrafficLane>();
    private int pathIndex = 0;
    public bool enableLocalAvoidance = false;

    //Lane change variables
    //private bool isChangingLane = false;
    //private TrafficLane targetChangeLane = null;
    //private TrafficLane originatingLane = null;
    //private float laneChangeProgress = 0.0f;
    //private float timeSinceLastLaneChange = 0.0f;
    //private bool recalculatePathOnLaneChange = true;

    // Stuck detection variables
    private float stuckTimer = 0.0f;
    private float ignoreAvoidanceTimer = 0.0f;
    private float currentAccumulatedWaitTime = 0.0f;
    
    /// <summary>
    /// Setup the vehicle
    /// </summary>
    void Start()
    {
        //Select a random car model
        int randomCarIndex = Random.Range(0, cars.Length);
        for (int i = 0; i < cars.Length; i++)
        {
            cars[i].SetActive(i == randomCarIndex);
        }

        //Doesnt have a starting lane disable it
        if (currentLane == null)
        {
            if (Time.frameCount > 2)
            {
                Debug.LogError("TrafficVehicle has no starting lane!", this);
                enabled = false;
            }
            return;
        }

        transform.position = currentLane.GetWorldPoint(0);
        transform.rotation = Quaternion.LookRotation(currentLane.GetWorldTangent(0));

        if (targetLane != null)
        {
            SetNewTarget(targetLane);
        }

        //timeSinceLastLaneChange = Random.Range(0f, laneChangeCooldown);
    }

    /// <summary>
    /// Sets a new target for the vehicle and calculates the path.
    /// </summary>
    public void SetNewTarget(TrafficLane newTarget)
    {
        targetLane = newTarget;
        if (currentLane == null || targetLane == null)
        {
            path.Clear();
            return;
        }

        //Finds path using A*
        path = FindPathAStar(currentLane, targetLane);
        pathIndex = 0;

    }

















    void Update()
    {


        if (currentLane == null) return;

        //timeSinceLastLaneChange += Time.deltaTime;

        float baseSpeed = Mathf.Min(maxSpeed, currentLane.speedLimit);
        float avoidanceSpeed = baseSpeed;
        RaycastHit hit;



        //Local Avoidance logic
        if (enableLocalAvoidance && Physics.Raycast(transform.position, transform.forward, out hit, detectionDistance, vehicleLayerMask))
        {
            float distance = hit.distance;
            float proximity = Mathf.InverseLerp(detectionDistance, stoppingDistance, distance);
            avoidanceSpeed = Mathf.Lerp(baseSpeed, 0, proximity);
        }


        //Car stuck helper code
        if (ignoreAvoidanceTimer > 0)
        {
            ignoreAvoidanceTimer -= Time.deltaTime;
            avoidanceSpeed = baseSpeed; //Disable avoidance
            stuckTimer = 0f; //Reset stuck timer
        }
        else
        {
            if (baseSpeed > 0.1f && avoidanceSpeed < 0.5f)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer >= 60.0f)
                {
                    //Car has been stuck for 60 seconds, force move for 1 second
                    ignoreAvoidanceTimer = 1.0f;
                    stuckTimer = 0f;
                }
            }
            else
            {
                stuckTimer = 0f;
            }
        }


        float effectiveSpeed = Mathf.Min(baseSpeed, avoidanceSpeed);

        float tangentLength = currentLane.GetWorldTangent(t).magnitude;
        if (tangentLength < 0.1f) tangentLength = 0.1f;

        t += (effectiveSpeed * Time.deltaTime) / tangentLength;






        if (t >= 1.0f)
        {
            float overflowT = t - 1.0f;

            if (path == null || path.Count == 0)
            {
                t = 1.0f;
                OnTargetReached?.Invoke(this);
            }
            else
            {
                pathIndex++;

                if (pathIndex < path.Count)
                {
                    TrafficLane nextLane = path[pathIndex];

                    // Red light
                    if (nextLane.isStopSignalActive)
                    {
                        t = 1.0f;
                        pathIndex--;
                        transform.position = currentLane.GetWorldPoint(1.0f);
                        transform.rotation = Quaternion.LookRotation(currentLane.GetWorldTangent(1.0f).normalized);
                        return;
                    }

                    //Accident or closed lane
                    if (nextLane.isClosed)
                    {
                        t = 1.0f;
                        pathIndex--;
                        SetNewTarget(targetLane);
                        transform.position = currentLane.GetWorldPoint(1.0f);
                        transform.rotation = Quaternion.LookRotation(currentLane.GetWorldTangent(1.0f).normalized);
                        return;
                    }

                    if (currentLane) currentLane.carsInLane.Remove(this.gameObject);
                    currentLane = nextLane;
                    currentLane.carsInLane.Add(this.gameObject);

                    t = overflowT;
                }
                else
                {
                    //Reached end of path
                    if (currentLane)
                    {
                        currentLane.carsInLane.Remove(this.gameObject);

                        if (currentLane.isIntersectionLane)
                        {
                            TrafficStatsManager.Instance.RegisterCarFinished(currentAccumulatedWaitTime);
                        }
                    }
                    currentLane = path.Last();
                    currentLane.carsInLane.Add(this.gameObject);
                    t = 1.0f;
                    path.Clear();

                    OnTargetReached?.Invoke(this);
                }
            }
        }

        Vector3 targetPoint;
        Vector3 tangent;

        targetPoint = currentLane.GetWorldPoint(t);
        tangent = currentLane.GetWorldTangent(t).normalized;

        transform.position = targetPoint;
        if (tangent != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(tangent);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

    }
  
    /// <summary>
    /// Begins the lane change maneuver.
    /// </summary>
    //private void StartLaneChange(TrafficLane target, bool recalculatePath = true)
    //{
    //    isChangingLane = true;
    //    targetChangeLane = target;
    //    originatingLane = currentLane;
    //    laneChangeProgress = 0.0f;
    //    recalculatePathOnLaneChange = recalculatePath;
    //}


    /// <summary>
    /// Finds path using A* Algorithm
    /// </summary>
    private List<TrafficLane> FindPathAStar(TrafficLane start, TrafficLane end)
    {
        //The set of nodes to be evaluated
        List<TrafficLane> openSet = new List<TrafficLane>();
        openSet.Add(start);

        //The map of navigated nodes.
        Dictionary<TrafficLane, TrafficLane> cameFrom = new Dictionary<TrafficLane, TrafficLane>();

        //Cost from start to node
        Dictionary<TrafficLane, float> gScore = new Dictionary<TrafficLane, float>();
        gScore[start] = 0;

        //Estimated cost from start to goal through node (gScore + Heuristic)
        Dictionary<TrafficLane, float> fScore = new Dictionary<TrafficLane, float>();
        fScore[start] = GetDistance(start, end);

        while (openSet.Count > 0)
        {
            //Get node in openSet with lowest fScore
            TrafficLane current = openSet.OrderBy(n => fScore.ContainsKey(n) ? fScore[n] : float.MaxValue).First();

            if (current == end)
            {
                return ReconstructPath(cameFrom, current);
            }

            openSet.Remove(current);

            //Get Neighbors
            List<TrafficLane> neighbors = new List<TrafficLane>();
            if (current.nextLanes != null) neighbors.AddRange(current.nextLanes);
            if (current.neighborLaneLeft != null) neighbors.Add(current.neighborLaneLeft);
            if (current.neighborLaneRight != null) neighbors.Add(current.neighborLaneRight);

            foreach (TrafficLane neighbor in neighbors)
            {
                if (neighbor == null || neighbor.isClosed) continue;

                float dist = GetDistance(current, neighbor);

                //cost to get to this neighbor
                float tentativeGScore = gScore[current] + dist;

                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    //This path is better than any previous one
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[neighbor] + GetDistance(neighbor, end);

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        //Return empty if no path found
        return new List<TrafficLane>();
    }

    /// <summary>
    /// Helper for A* Heuristic (Euclidean Distance between midpoints)
    /// </summary>
    private float GetDistance(TrafficLane a, TrafficLane b)
    {
        return Vector3.Distance(a.GetWorldPoint(0.5f), b.GetWorldPoint(0.5f));
    }

    /// <summary>
    /// Reconstructs the path
    /// </summary>
    private List<TrafficLane> ReconstructPath(Dictionary<TrafficLane, TrafficLane> cameFrom, TrafficLane current)
    {
        List<TrafficLane> totalPath = new List<TrafficLane>();
        totalPath.Add(current);
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            totalPath.Add(current);
        }
        totalPath.Reverse();
        return totalPath;
    }







}