using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameState : MonoBehaviour
{
    public static float masterVolume = 0.1f;
    public static int numberOfSwitchesThisGame = 0;
    public static int numberOfDeathsThisGame = 0;

    public void UpdateMasterVolume(float value)
    {
        masterVolume = value;
    }

    public static void ResetGame()
    {
        numberOfSwitchesThisGame = 0;
        numberOfDeathsThisGame = 0;
    }
}
