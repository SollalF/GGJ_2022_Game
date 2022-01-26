using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvolutionTracker : MonoBehaviour
{
    [SerializeField] private float evolutionSpeed = 1f;

    [SerializeField] private GameObject lastSwitchWaypoint;

    // Status variables
    public float evolutionLevel { get; private set; } = 0f;
    private float maxDistanceSinceLastSwitch = 0f;
    // Cache variables
    SpriteRenderer mySpriteRenderer;
    TextDisplayer myTextDisplayer;

    // Start is called before the first frame update
    void Start()
    {
        myTextDisplayer = GetComponent<TextDisplayer>();
        mySpriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateEvolutionLevel();
    }

    private void UpdateEvolutionLevel()
    {
        if (maxDistanceSinceLastSwitch < transform.position.x - lastSwitchWaypoint.transform.position.x)
        {
            maxDistanceSinceLastSwitch = transform.position.x - lastSwitchWaypoint.transform.position.x;
            evolutionLevel = maxDistanceSinceLastSwitch * evolutionSpeed;
            Color newColor = Color.red;
            newColor.r -= evolutionLevel;
            mySpriteRenderer.color = newColor;
        }
    }

    public void UpdateLastSwitchWaypoint(GameObject waypoint)
    {
        maxDistanceSinceLastSwitch = 0;
        evolutionLevel = 0;
        lastSwitchWaypoint = waypoint;
        mySpriteRenderer.color = Color.red;
        myTextDisplayer.lastTextOutput = 0;
    }
}
