//using UnityEngine;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;

///// <summary>
///// A manager to create random, temporary accidents on the road network.
///// </summary>
//public class AccidentManager : MonoBehaviour
//{
//    public GameObject accidentPrefab;
//    public float accidentDuration = 15.0f;
//    public float timeBetweenAccidents = 10.0f;
//    [Range(0, 1)]
//    public float accidentChance = 0.5f;

//    private List<TrafficLane> allLanes;
//    private HashSet<TrafficLane> closedLanes = new HashSet<TrafficLane>();
//    public Transform mainTrafficSystemParent;

//    /// <summary>
//    /// Setup the manager and start the main sequence
//    /// </summary>
//    private IEnumerator Start()
//    {
//        if (mainTrafficSystemParent == null)
//        {
//            yield break;
//        }

//        float timeout = 5f;
//        float elapsed = 0f;
//        TrafficLane[] lanes = null;

//        while ((lanes == null || lanes.Length == 0) && elapsed < timeout)
//        {
//            lanes = mainTrafficSystemParent.GetComponentsInChildren<TrafficLane>(true);
//            if (lanes != null && lanes.Length > 0) break;

//            elapsed += Time.deltaTime;
//            yield return null;
//        }

//        if (lanes == null || lanes.Length == 0)
//        {
//            enabled = false;
//            yield break;
//        }

//        allLanes = lanes.ToList();

  

//        StartCoroutine(AccidentCheckLoop());
//    }
//    /// <summary>
//    /// Main sequence - to create accidents
//    /// </summary>
//    private IEnumerator AccidentCheckLoop()
//    {
//        while (true)
//        {
//            yield return new WaitForSeconds(timeBetweenAccidents);
//            if (Random.value <= accidentChance)
//            {
//                StartRandomAccident();
//            }
//        }
//    }
//    /// <summary>
//    /// Create random accident
//    /// </summary>
//    public void StartRandomAccident()
//    {
//        var availableLanes = allLanes.Where(lane => !closedLanes.Contains(lane)).ToList();
//        if (availableLanes.Count == 0)
//        {
//            return;
//        }
//        TrafficLane lane = availableLanes[Random.Range(0, availableLanes.Count)];
//        StartCoroutine(ManageAccident(lane));
//    }
//    /// <summary>
//    /// Manage a single accident
//    /// Start - and end
//    /// </summary>
//    private IEnumerator ManageAccident(TrafficLane lane)
//    {

//        //Close the lane for the network
//        lane.isClosed = true;
//        closedLanes.Add(lane);

//        //Create the physical obstacle
//        GameObject obstacle = null;
//        if (accidentPrefab != null)
//        {
//            Vector3 pos = lane.GetWorldPoint(0.5f);
//            Quaternion rot = Quaternion.LookRotation(lane.GetWorldTangent(0.5f));
//            obstacle = Instantiate(accidentPrefab, pos, rot);

//        }

//        yield return new WaitForSeconds(accidentDuration);

//        //Clear accident
//        lane.isClosed = false;
//        closedLanes.Remove(lane);
//        if (obstacle != null)
//        {
//            Destroy(obstacle);
//        }
//    }
//}