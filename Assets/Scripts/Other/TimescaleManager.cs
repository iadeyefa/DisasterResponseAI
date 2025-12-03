using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

/// <summary>
/// Manages changes the time scale of the game based on user selection from a dropdown menu.
/// </summary>
public class TimescaleManager : MonoBehaviour
{
    public GameObject panel;
    public TMP_Dropdown timescaleDropdown;
    public void UpdateTime()
    {
        if(timescaleDropdown.value == 0) {             
            Time.timeScale = 20f; 
        }
        else if(timescaleDropdown.value == 1)
        {
            Time.timeScale = 10f; 
        }
        else if(timescaleDropdown.value == 2)
        {
            Time.timeScale = 1f;
        }
    }

    private void Start()
    {
        Time.timeScale = 40f;
    }
    private float timer = 40;
    private void Update()
    {
        if(panel.gameObject.activeSelf)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                panel.SetActive(false);
                UpdateTime();
            }
        }

    }
}
