using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages changes the time scale of the game based on user selection from a dropdown menu.
/// </summary>
public class TimescaleManager : MonoBehaviour
{
    public TMP_Dropdown timescaleDropdown;
    public void UpdateTime()
    {
        if(timescaleDropdown.value == 0) {             
            Time.timeScale = 1f;
        }
        else if(timescaleDropdown.value == 1)
        {
            Time.timeScale = 10f; 
        }
        else if(timescaleDropdown.value == 2)
        {
            Time.timeScale = 40f; 
        }
    }
}
