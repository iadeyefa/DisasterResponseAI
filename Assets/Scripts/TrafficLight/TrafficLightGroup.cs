using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public enum TrafficMode
{
    BasicLoop, 
    SmartAI 
}
/// <summary>
/// Intersection Traffic Lights Manager
/// </summary>
public class TrafficLightGroup : MonoBehaviour
{
    public TrafficMode currentMode = TrafficMode.SmartAI;

    [System.Serializable]
    public class LaneGroup
    {
        public string groupName;
        public List<TrafficLane> lanes = new List<TrafficLane>();
        public TrafficLight[] onTrafficLights;
    }

    public List<LaneGroup> lightGroups = new List<LaneGroup>();

    private float fixedGreenDuration = 20.0f;
    private float minGreenTime = 5.0f;
    private float maxGreenTime = 30.0f;
    private float weightPerCar = 1.0f;
    private float weightPerSecondWaited = 0.5f;
    public float totalWastedGreenTime = 0f;
    private float yellowLightDuration = 3.0f;
    private float allRedSafetyTime = 1.0f;

    private int currentGroupIndex = 0;
    public float currentIntersectionPainScore = 0f;

    void Start()
    {
        if (lightGroups.Count == 0) return;

        //Initialize all to Red
        foreach (var group in lightGroups) SetGroupState(group, LightColor.Red);

        StartCoroutine(TrafficCycleRoutine());
    }

    /// <summary>
    /// Call this from UI Button to switch modes dynamically.
    /// </summary>
    public void ChangeMode(TrafficMode mode)
    {
        currentMode = mode;
    }

    /// <summary>
    /// Main loop logic for cycling through traffic lights
    /// </summary>
    private IEnumerator TrafficCycleRoutine()
    {
        while (true)
        {
            //Pick next group based on mode
            if (currentMode == TrafficMode.BasicLoop)
            {
                currentGroupIndex = (currentGroupIndex + 1) % lightGroups.Count;
            }
            else
            {
                currentGroupIndex = SelectNextSmartGroup(currentGroupIndex);
            }

            LaneGroup currentGroup = lightGroups[currentGroupIndex];

            //Green light for selected group
            SetGroupState(currentGroup, LightColor.Green);

            //Wait for green duration based on mode
            float timeElapsed = 0f;
            float limit = (currentMode == TrafficMode.BasicLoop) ? fixedGreenDuration : maxGreenTime;

            while (timeElapsed < limit)
            {
                CalculateGlobalPainScore();
                UpdateWastedTime(currentGroup); 

                if (currentMode == TrafficMode.SmartAI)
                {
                    // Smart Mode: Early Exit if empty
                    if (timeElapsed > minGreenTime && GetTotalCarsInGroup(currentGroup) == 0)
                    {
                        break;
                    }
                }

                timeElapsed += Time.deltaTime;
                yield return null;
            }

            //yellow light for current group
            SetGroupState(currentGroup, LightColor.Yellow);
            yield return new WaitForSeconds(yellowLightDuration);

            //red light for current group
            SetGroupState(currentGroup, LightColor.Red);
            yield return new WaitForSeconds(allRedSafetyTime);
        }
    }


    public void ResetStats()
    {
        totalWastedGreenTime = 0f;
    }

    /// <summary>
    /// Selects the next traffic light group to turn green based on a scoring system.
    /// </summary>
    private int SelectNextSmartGroup(int currentlyActiveIndex)
    {
        int bestIndex = -1;
        float highestScore = -1f;

        //check all groups to find the best score
        for (int i = 0; i < lightGroups.Count; i++)
        {
            float score = CalculateGroupScore(lightGroups[i]);

            if (score > highestScore)
            {
                highestScore = score;
                bestIndex = i;
            }
        }

        //if lanes are empty, just go to the next in line
        if (highestScore <= 0.1f)
        {
            return (currentlyActiveIndex + 1) % lightGroups.Count;
        }

        //if the best is the currently active, try to find a second best
        if (bestIndex == currentlyActiveIndex)
        {
            int secondBestIndex = -1;
            float secondBestScore = -1f;

            for (int i = 0; i < lightGroups.Count; i++)
            {
                if (i == currentlyActiveIndex) continue;

                float s = CalculateGroupScore(lightGroups[i]);
                if (s > 0.1f && s > secondBestScore)
                {
                    secondBestScore = s;
                    secondBestIndex = i;
                }
            }

            if (secondBestIndex != -1) return secondBestIndex;
            return currentlyActiveIndex; //No one else waiting, keep green
        }

        return bestIndex;
    }
    /// <summary>
    /// Calculates the score for a lane group based on number of cars and their wait times.
    /// </summary>
    private float CalculateGroupScore(LaneGroup group)
    {
        int carCount = 0;
        float totalWaitTime = 0f;

        foreach (var lane in group.lanes)
        {
            if (lane == null) continue;
            carCount += lane.GetCarCount();

            foreach (GameObject carObj in lane.carsInLane)
            {
                if (carObj != null)
                {
                    var tracker = carObj.GetComponent<CarTrafficTracker>();
                    if (tracker != null) totalWaitTime += tracker.currentWaitTime;
                }
            }
        }

        return (carCount * weightPerCar) + (totalWaitTime * weightPerSecondWaited);
    }


    private int GetTotalCarsInGroup(LaneGroup group)
    {
        int count = 0;
        foreach (var lane in group.lanes) if (lane != null) count += lane.GetCarCount();
        return count;
    }

    private void CalculateGlobalPainScore()
    {
        float total = 0;
        foreach (var group in lightGroups) total += CalculateGroupScore(group);
        currentIntersectionPainScore = total;
    }

    private void SetGroupState(LaneGroup group, LightColor color)
    {
        foreach (var lane in group.lanes)
        {
            if (lane != null) lane.isStopSignalActive = (color == LightColor.Red);
        }
        foreach (var light in group.onTrafficLights)
        {
            if (light != null) light.ChangeLight(color);
        }
    }


    private void UpdateWastedTime(LaneGroup activeGroup)
    {
        //If the light is green but no cars are present, we are wasting time
        if (GetTotalCarsInGroup(activeGroup) == 0)
        {
            totalWastedGreenTime += Time.deltaTime;
        }
    }
}