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
    public TextMeshProUGUI maxWaitText;     
    public TextMeshProUGUI throughputText;   
    public TextMeshProUGUI wastedTimeText;  
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


    void Start()
    {
        startTime = Time.time;
        controllers = FindObjectsOfType<TrafficLightGroup>();
    }



    void Update()
    {
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

        for (int i = 0; i < controllers.Length; i++)
        {
            controllers[i].ChangeMode(newMode);
        }

        ResetAllStats();
    }
    /// <summary>
    /// Car calls this function when it leaves an intersection
    /// </summary>
    public void RegisterCarFinished(float finalWaitTime)
    {
        carsFinishedCount++;
        sumOfAllWaitTimes += finalWaitTime; 
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
        //max wait time
        float maxWait = 0f;
        foreach (var car in allVehicles)
        {
            if (car != null && car.isActiveAndEnabled)
            {
                if (car.tracker.GetTotalWaitTime() > maxWait)
                {
                    maxWait = car.tracker.GetTotalWaitTime();
                }
            }
        }
        if (maxWaitText) maxWaitText.text = $"Max Wait: {maxWait:F1}s";

        //Throughput
        float timeRunning = Time.time - startTime;
        float cpm = (carsFinishedCount / timeRunning) * 60.0f;
        if (throughputText) throughputText.text = $"Throughput: {cpm:F1} cars/min";

        //Wasted time
        float totalWaste = 0f;
        foreach(var con in controllers) totalWaste += con.totalWastedGreenTime;
        if(wastedTimeText) wastedTimeText.text = $"Wasted Green: {totalWaste:F1}s";

        //average time
        float avgWait = (carsFinishedCount > 0) ? (sumOfAllWaitTimes / carsFinishedCount) : 0f;
        avgWaitText.text = $"Avg Wait: {avgWait:F1}s";




       
    }
    /// <summary>
    /// Reset all stats
    /// </summary>
    private void ResetAllStats()
    {
        startTime = Time.time;
        carsFinishedCount = 0;

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

    public float GetCurrentMaxWait()
    {
        float max = 0f;
        foreach (var car in allVehicles)
            if (car != null)
                max = Mathf.Max(max, car.tracker.GetTotalWaitTime());
        return max;
    }

    public float GetCurrentAvgWait()
    {
        if (carsFinishedCount == 0) return 0f;
        return sumOfAllWaitTimes / carsFinishedCount;
    }

    public float GetCurrentThroughput()
    {
        float runtime = Time.time - startTime;
        return (carsFinishedCount / runtime) * 60f;
    }

    public float GetCurrentWaste()
    {
        float total = 0f;
        foreach (var c in controllers)
            total += c.totalWastedGreenTime;
        return total;
    }

}