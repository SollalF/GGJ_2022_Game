using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameOverManager : MonoBehaviour
{

    [SerializeField] TMP_Text deathCounterText;
    [SerializeField] TMP_Text switchCounterText;
    private void Start()
    {
        deathCounterText.text = "Deaths : " + GameState.numberOfDeathsThisGame;
        switchCounterText.text = "Dimension switches : " + GameState.numberOfSwitchesThisGame;
    }
}
