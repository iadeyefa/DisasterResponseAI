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

    ///The VehicleSpawner listens to this to assign a new target.
    public System.Action<TrafficVehicle> OnTargetReached;


    //Progress along the Bezier curve 0 to 1
    private float t = 0.0f;

    // The calculated path to the target.
    private List<TrafficLane> path = new List<TrafficLane>();
    private int pathIndex = 0;
    public bool enableLocalAvoidance = false;

    // Stuck detection variables
    private float stuckTimer = 0.0f;
    private float ignoreAvoidanceTimer = 0.0f;
    private float currentAccumulatedWaitTime = 0.0f;

    // Public property for Max Wait Time calculation in TrafficStatsManager
    public float CurrentWaitTime => currentAccumulatedWaitTime;

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

        //Check for traffic light stop signal
        if (pathIndex + 1 < path.Count && path[pathIndex + 1].isStopSignalActive)
        {
            if (t >= 0.8f)
            {
                avoidanceSpeed = 0f;
            }
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
                if (stuckTimer >= 10.0f && currentLane.isIntersectionLane)
                {
                    ignoreAvoidanceTimer = 1.0f;
                    stuckTimer = 0f;
                }
                if (stuckTimer >= 50.0f && !currentLane.isIntersectionLane)
                {
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

        //wait time
        if (effectiveSpeed < 0.1f && maxSpeed > 0.1f)
        {
            currentAccumulatedWaitTime += Time.deltaTime;
        }

        float tangentLength = currentLane.GetWorldTangent(t).magnitude;
        if (tangentLength < 0.1f) tangentLength = 0.1f;

        t += (effectiveSpeed * Time.deltaTime) / tangentLength;


        if (t >= 1.0f)
        {
            float overflowT = t - 1.0f;

            if (path == null || path.Count == 0 || pathIndex >= path.Count - 1)
            {
                t = 1.0f;

                //final dest reached
                if (currentLane)
                {
                    currentLane.carsInLane.Remove(this.gameObject);
                    TrafficStatsManager.Instance.RegisterCarThroughIntersection(currentAccumulatedWaitTime);
                    currentAccumulatedWaitTime = 0f;
                }

                OnTargetReached?.Invoke(this);
            }
            else
            {
                pathIndex++;

                TrafficLane nextLane = path[pathIndex];

                //stop logic
                if (nextLane.isStopSignalActive || nextLane.isClosed)
                {
                    t = 1.0f;
                    pathIndex--;
                    transform.position = currentLane.GetWorldPoint(1.0f);
                    transform.rotation = Quaternion.LookRotation(currentLane.GetWorldTangent(1.0f).normalized);
                    if (nextLane.isClosed) SetNewTarget(targetLane);
                    return;
                }

                //lane transition
                if (currentLane)
                {
                    currentLane.carsInLane.Remove(this.gameObject);

                    //register wait only when leaving intersection
                    if (currentLane.isIntersectionLane)
                    {
                        TrafficStatsManager.Instance.RegisterCarThroughIntersection(currentAccumulatedWaitTime);
                        currentAccumulatedWaitTime = 0f;
                    }
                }

                currentLane = nextLane;
                currentLane.carsInLane.Add(this.gameObject);
                t = overflowT;
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

    private List<TrafficLane> FindPathAStar(TrafficLane start, TrafficLane end)
    {
        List<TrafficLane> openSet = new List<TrafficLane>();
        openSet.Add(start);

        Dictionary<TrafficLane, TrafficLane> cameFrom = new Dictionary<TrafficLane, TrafficLane>();



        Dictionary<TrafficLane, float> gScore = new Dictionary<TrafficLane, float>();
        gScore[start] = 0;
        Dictionary<TrafficLane, float> fScore = new Dictionary<TrafficLane, float>();
        fScore[start] = GetDistance(start, end);

        while (openSet.Count > 0)
        {
            TrafficLane current = openSet.OrderBy(n => fScore.ContainsKey(n) ? fScore[n] : float.MaxValue).First();

            if (current == end) return ReconstructPath(cameFrom, current);

            openSet.Remove(current);

            List<TrafficLane> neighbors = new List<TrafficLane>();


            if (current.nextLanes != null) neighbors.AddRange(current.nextLanes);
            if (current.neighborLaneLeft != null) neighbors.Add(current.neighborLaneLeft);
            if (current.neighborLaneRight != null) neighbors.Add(current.neighborLaneRight);

            foreach (TrafficLane neighbor in neighbors)
            {
                if (neighbor == null || neighbor.isClosed) continue;
                float dist = GetDistance(current, neighbor);
                float tentativeGScore = gScore[current] + dist;

                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
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

        return new List<TrafficLane>();
    }

    private float GetDistance(TrafficLane a, TrafficLane b)
    {
        return Vector3.Distance(a.GetWorldPoint(0.5f), b.GetWorldPoint(0.5f));
    }

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