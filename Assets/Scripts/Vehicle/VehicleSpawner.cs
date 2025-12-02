using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Spawns a number of vehicles onto the road network and gives them random destinations. When a vehicle reaches its destination, it is assigned a new one.
/// </summary>
public class VehicleSpawner : MonoBehaviour
{
    [SerializeField] private Transform carParent;
    [SerializeField] private GameObject vehiclePrefab;
    [SerializeField] private int numberOfVehicles = 20;
    [SerializeField] private Transform mainTrafficSystemParent;
    //All lanes in the scene
    private List<TrafficLane> allLanes;

    /// <summary>
    /// Setup - Get all lanes and spawn the vehicles
    /// </summary>
    private IEnumerator Start()
    {
    
        TrafficLane[] lanes = mainTrafficSystemParent.GetComponentsInChildren<TrafficLane>(true);

        if (lanes == null || lanes.Length == 0)
        {
            Debug.LogError("No TrafficLanes foun");
            yield break;
        }

        allLanes = lanes.ToList();

        SpawnVehicles();
    }
    /// <summary>
    /// Spawn Vehicles
    /// </summary>
    private void SpawnVehicles()
    {
        for (int i = 0; i < numberOfVehicles; i++)
        {
            //Get random lane
            TrafficLane startLane = GetRandomLane();

            //Create the vehicle on a random point on the lane
            Vector3 spawnPos = startLane.GetWorldPoint(UnityEngine.Random.Range(0.0f,1.0f));
            Quaternion spawnRot = Quaternion.LookRotation(startLane.GetWorldTangent(0).normalized);
            GameObject newVehicleObj = Instantiate(vehiclePrefab, spawnPos, spawnRot,carParent);
            newVehicleObj.name = $"Vehicle_{i}";

            TrafficVehicle vehicle = newVehicleObj.GetComponent<TrafficVehicle>();


            vehicle.currentLane = startLane;

            //Set target lane and call back event
            vehicle.targetLane = GetRandomLane(startLane); 
            vehicle.OnTargetReached += HandleVehicleTargetReached;
        }
    }

    /// <summary>
    /// This method is called by a vehicle when it reaches its target.
    /// </summary>
    private void HandleVehicleTargetReached(TrafficVehicle vehicle)
    {



        //Get a new target
        TrafficLane newTarget = GetRandomLane(vehicle.currentLane);
        vehicle.SetNewTarget(newTarget);
    }

    ///<summary>
    ///Helper function to get a random lane
    ///</summary>
    private TrafficLane GetRandomLane(TrafficLane excludeLane = null)
    {



        if (allLanes.Count <= 1)
        {
            return allLanes[0];
        }




        TrafficLane newLane;
        do
        {
            newLane = allLanes[UnityEngine.Random.Range(0, allLanes.Count)];
        }
        while (newLane == excludeLane);

        return newLane;
    }
}