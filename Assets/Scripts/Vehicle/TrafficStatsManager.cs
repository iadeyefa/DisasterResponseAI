using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

/// <summary>
/// States manager to display the data
/// </summary>
public class TrafficStatsManager : MonoBehaviour
{
    public static TrafficStatsManager Instance;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }


    [Header("UI References")]
    public TextMeshProUGUI throughputText;
    public TextMeshProUGUI avgWaitText;

    public Toggle overlayToggle;
    public Toggle modeToggle;
    //private vars
    private List<TrafficVehicle> allVehicles = new List<TrafficVehicle>();
    private int carsFinishedCount = 0;
    private float startTime;
    private TrafficLightGroup[] controllers;
    private float timer = 0f;
    private float uiUpdateRate = 0.5f;
    private float sumOfAllWaitTimes = 0f;

    private List<KeyValuePair<float, float>> finishedCarData = new List<KeyValuePair<float, float>>();

    void Start()
    {
        startTime = Time.time;
        
    }


    void Update()
    {
        if(controllers == null || controllers.Length == 0)
        {
            controllers = FindObjectsOfType<TrafficLightGroup>();
        }
        timer += Time.deltaTime;
        if (timer >= uiUpdateRate)
        {
            if (allVehicles.Count == 0)
            {
                RefreshCarList();
            }

            CalculateStats();
            timer = 0f;
        }
    }

    /// <summary>
    /// Switch between Basic and Smart AI modes
    /// </summary>
    public void ChangeMode()
    {
        TrafficMode newMode = modeToggle.isOn
            ? TrafficMode.SmartAI
            : TrafficMode.BasicLoop;


        float maxOffset = (newMode == TrafficMode.BasicLoop) ? 20.0f : 0f;

        for (int i = 0; i<controllers.Length; i++)
        {
            float randomDelay = UnityEngine.Random.Range(0f, maxOffset);
            controllers[i].ChangeMode(newMode, randomDelay);
        }

        ResetAllStats();
    }

    /// <summary>
    /// Car calls this function when it leaves a monitored lane segment.
    /// </summary>
    public void RegisterCarThroughIntersection(float finalWaitTime)
    {
        carsFinishedCount++;
        sumOfAllWaitTimes += finalWaitTime;
        // Record the time of completion and the car's final wait time
        finishedCarData.Add(new KeyValuePair<float, float>(Time.time, finalWaitTime));
    }

    /// <summary>
    /// Calculates throughput (cars/min) and average wait time for the last 60 seconds
    /// </summary>
    private (float throughput, float avgWait) GetLastMinuteStats()
    {
        float currentTime = Time.time;
        float cutoffTime = currentTime - 60.0f;
        float sumOfLastMinuteWaitTimes = 0f;

        // Efficiently remove cars older than the cutoff time (60 seconds ago)
        while (finishedCarData.Count > 0 && finishedCarData[0].Key < cutoffTime)
        {
            finishedCarData.RemoveAt(0);
        }

        int carsInLastMinute = finishedCarData.Count;

        // Calculate the sum of wait times for the remaining cars
        for (int i = 0; i < carsInLastMinute; i++)
        {
            sumOfLastMinuteWaitTimes += finishedCarData[i].Value;
        }

        float lastMinuteThroughput = carsInLastMinute;
        float lastMinuteAvgWait = (carsInLastMinute > 0) ? (sumOfLastMinuteWaitTimes / carsInLastMinute) : 0f;

        return (lastMinuteThroughput, lastMinuteAvgWait);
    }


    /// <summary>
    /// Gets all the cars in the scene
    /// </summary>
    public void RefreshCarList()
    {
        allVehicles = FindObjectsOfType<TrafficVehicle>().ToList();
    }

    /// <summary>
    /// Calculate and update all stats
    /// </summary>
    private void CalculateStats()
    {
  

        //Calculate Last Minute Throughput and Avg Wait Time
        (float lastMinThroughput, float lastMinAvgWait) = GetLastMinuteStats();

        //Throughput UI Update
        if (throughputText) throughputText.text = $"{lastMinThroughput:F1} cars/min";

        //Average Wait Time UI Update (Last Minute)
        avgWaitText.text = $"{lastMinAvgWait:F1}s";
    }

    /// <summary>
    /// Reset all stats
    /// </summary>
    private void ResetAllStats()
    {
        startTime = Time.time;
        carsFinishedCount = 0;
        sumOfAllWaitTimes = 0f;
        finishedCarData.Clear(); // Clears the list for last-minute tracking

        foreach (var con in controllers)
        {
            con.ResetStats();
        }

        RefreshCarList();
        foreach (var car in allVehicles)
        {
            if (car != null) car.tracker.ResetWaitTime();
        }

        CalculateStats();
    }




}