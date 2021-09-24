using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieScript : MonoBehaviour
{
    private Canvas can;
    private GameObject player;
    private QuickTimeEvents currentEvent;

    public int numWins = 0;
    public int totalNumQTE = 1;
    public GameObject quickTimeBar;
    public AnimationCurve zombieForce;

    void Start()
    {
        GameObject[] results = GameObject.FindGameObjectsWithTag("Player");
        player = results[0];

        results = GameObject.FindGameObjectsWithTag("Canvas");
        can = results[0].GetComponent<Canvas>();

        currentEvent = new SpamButtonEvent(player, this, can);
    }

    // Update is called once per frame
    void Update()
    {
        if (totalNumQTE > 0)
        {
            currentEvent?.Update();
        }
    }

    public void onComplete(bool playerWon)
    {
        totalNumQTE--;
        numWins += (playerWon) ? 1 : 0;
        currentEvent = null;
    }

    public void generateNewEvent()
    {
        currentEvent = null;
        int number = UnityEngine.Random.Range(0, typeOfQTE.GetNames(typeof(typeOfQTE)).Length);
        //if (number == 0)
        //{
        //    currentEvent = new SpamButtonEvent(player, this, can);
        //}

        //if (number == 1)
        //{
        //    currentEvent = new SpamButtonEvent(player, this, can);
        //}

        currentEvent = new SpamButtonEvent(player, this, can);
    }
}
