using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextDisplayer : MonoBehaviour
{
    // Serialized Variables
    [SerializeField] private string[] topsideText;
    [SerializeField] private string[] botsideText;

    [SerializeField] private float[] distanceForTextsOutputs;

    public int lastTextOutput { get; set; } = 0;


    // Cache variables
    PlayerController myPlayerController;

    // Start is called before the first frame update
    void Start()
    {
        myPlayerController = GetComponent<PlayerController>();
    }

    // Update is called once per frame
    void Update()
    {
        {
            Debug.Log(myPlayerController.isTovSide ? topsideText[lastTextOutput++] : botsideText[lastTextOutput++]);
        }
    }
}
