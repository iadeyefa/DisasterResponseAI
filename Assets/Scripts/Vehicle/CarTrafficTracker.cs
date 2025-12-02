using UnityEngine;

/// <summary>
/// Handles tracking how long a car has been waiting in traffic
/// </summary>
public class CarTrafficTracker : MonoBehaviour
{
    public float currentWaitTime = 0f;
    public float totalWaitTime = 0f;
    private Rigidbody rb;


    public float GetCurrentWaitTime() {         
        return currentWaitTime;
    }

    public float GetTotalWaitTime()
    {
        return totalWaitTime;
    }
    public void ResetWaitTime()
    {
        totalWaitTime = 0;
        currentWaitTime = 0;
    }


    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        //if the car is barely moving = waiting at a traffic light
        if (rb.linearVelocity.magnitude < 0.5f)
        {
            currentWaitTime += Time.deltaTime;
            totalWaitTime += Time.deltaTime;
        }
        else
        {
            currentWaitTime = 0;
        }
    }
}