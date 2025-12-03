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
    public TrafficMode currentMode = TrafficMode.BasicLoop;

    [System.Serializable]
    public class LaneGroup
    {
        public string groupName;
        public List<TrafficLane> lanes = new List<TrafficLane>();
        public TrafficLight[] onTrafficLights;
    }

    public List<LaneGroup> lightGroups = new List<LaneGroup>();

    private float fixedGreenDuration = 50.0f; 
    private float minGreenTime = 2.0f;
    private float maxGreenTime = 20.0f; 
    private float weightPerCar = 1.0f;
    private float weightPerSecondWaited = 2.5f; 

    private float yellowLightDuration = 3.0f;
    private float allRedSafetyTime = 1.0f;

    private int currentGroupIndex = 0;
    public float currentIntersectionPainScore = 0f;

    public bool useRandomStagger = true;
    public float minStaggerDelay = 0f;
    public float maxStaggerDelay = 20f;

    void Start()
    {
        currentMode = TrafficMode.BasicLoop;
        if (lightGroups.Count == 0) return;

        foreach (var group in lightGroups) SetGroupState(group, LightColor.Red);

        StopAllCoroutines();


        float initialDelay = 0f;
        if (useRandomStagger)
        {
            initialDelay = Random.Range(minStaggerDelay, maxStaggerDelay);
        }

        StartCoroutine(TrafficCycleRoutine(initialDelay));


    }

    /// <summary>
    /// Call this from UI Button to switch modes dynamically.
    /// </summary>
    public void ChangeMode(TrafficMode mode)
    {
        if (lightGroups.Count == 0)
        {
            return;
        }

        if (currentMode != mode)
        {
            currentMode = mode;
            StopAllCoroutines();
            StartCoroutine(TrafficCycleRoutine(0f));
        }
    }

    /// <summary>
    /// Main loop logic for cycling through traffic lights
    /// </summary>
    private IEnumerator TrafficCycleRoutine(float startupDelay = 0f)
    {
        if (lightGroups.Count == 0) yield break;

        if (startupDelay > 0f)
        {
            yield return new WaitForSeconds(startupDelay);
        }

        currentGroupIndex = 0;

        foreach (var group in lightGroups) SetGroupState(group, LightColor.Red);

        while (true)
        {
            int previousGroupIndex = currentGroupIndex;

            if (currentMode == TrafficMode.BasicLoop)
            {
                currentGroupIndex = (currentGroupIndex + 1) % lightGroups.Count;
            }
            else 
            {
                currentGroupIndex = SelectNextSmartGroup(currentGroupIndex);
            }


            //turn prev yellow then red
            LaneGroup previousGroup = lightGroups[previousGroupIndex];

            if (currentGroupIndex != previousGroupIndex)
            {
                SetGroupState(previousGroup, LightColor.Yellow);
                yield return new WaitForSeconds(yellowLightDuration);

                SetGroupState(previousGroup, LightColor.Red);
                yield return new WaitForSeconds(allRedSafetyTime);
            }

            //green phase
            LaneGroup currentGroup = lightGroups[currentGroupIndex];

            SetGroupState(currentGroup, LightColor.Green);

            float timeElapsed = 0f;
            float limit = (currentMode == TrafficMode.BasicLoop) ? fixedGreenDuration : maxGreenTime;

            while (timeElapsed < limit)
            {
                CalculateGlobalPainScore();

                if (currentMode == TrafficMode.SmartAI)
                {
                    //Early Exit if empty
                    if (timeElapsed > minGreenTime && GetTotalCarsInGroup(currentGroup) == 0)
                    {
                        break;
                    }

                    //Terminate if high pain elsewhere
                    if (timeElapsed > minGreenTime)
                    {
                        float nextBestScore = GetHighestScoreExcluding(currentGroupIndex);
                        if (nextBestScore > 10.0f && GetTotalCarsInGroup(currentGroup) < 2) 
                        {
                            break;
                        }
                    }
                }

                timeElapsed += Time.deltaTime;
                yield return null;
            }

        }
    }


    public void ResetStats()
    {
        
    }

    /// <summary>
    /// Selects the next traffic light group to turn green based on a scoring system.
    /// </summary>
    private int SelectNextSmartGroup(int currentlyActiveIndex)
    {
        if (lightGroups.Count == 0) return 0;

        int bestIndex = -1;
        float highestScore = -1f;

        //If the winning score is less than 0.1, cycle to the next group
        if (highestScore <= 0.1f)
        {
            return (currentlyActiveIndex + 1) % lightGroups.Count;
        }

        //If the best is the currently active
        if (bestIndex == currentlyActiveIndex)
        {
            //if current has no cars, force switch
            if (GetTotalCarsInGroup(lightGroups[currentlyActiveIndex]) == 0)
            {
                return (currentlyActiveIndex + 1) % lightGroups.Count;
            }

            //second best logic
            int secondBestIndex = -1;
            float secondBestScore = -1f;

            if (secondBestIndex != -1) return secondBestIndex;

            return currentlyActiveIndex;
        }

        return bestIndex;
    }

    /// <summary>
    /// Finds the highest score among groups NOT currently active.
    /// </summary>
    private float GetHighestScoreExcluding(int excludedIndex)
    {
        float maxScore = 0f;
        for (int i = 0; i < lightGroups.Count; i++)
        {
            if (i == excludedIndex) continue;

            float score = CalculateGroupScore(lightGroups[i]);
            if (score > maxScore)
            {
                maxScore = score;
            }
        }
        return maxScore;
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


}