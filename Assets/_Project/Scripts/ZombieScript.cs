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

    bool searchForHuman = true;
    public float senseDistance = 2.0f;
    private Player_pull_script puller;

    void OnDestroy()
    {
        //deconstructor
    }

    void Start()
    {
        GameObject[] results = GameObject.FindGameObjectsWithTag("Player");
        player = results[0];
        puller = player.GetComponent<Player_pull_script>();

        results = GameObject.FindGameObjectsWithTag("Canvas");
        can = results[0].GetComponent<Canvas>();
        quickTimeBar = can.transform.GetChild(0).gameObject;
    }

    void spottedPlayer()
    {
        generateNewEvent();
    }

    // Update is called once per frame
    void Update()
    {
        if (searchForHuman)
        {
            Vector3 dir = (gameObject.transform.position - player.transform.position);
            dir.y = 0;

            //Debug.Log(dir.magnitude);

            if (dir.magnitude <= senseDistance)
            {
                
                //Debug.Log("seen!");
                //Debug.Log(puller);
                if(!puller.UnderAttack)
                {
                    puller.Besieged(this, true);
                    
                    searchForHuman = false;
                    spottedPlayer();
                }
            }
        }
        else
        {
            if (totalNumQTE > 0)
            {
                currentEvent?.Update();
            }
        }
    }

    public void onComplete(bool playerWon)
    {
        totalNumQTE--;
        numWins += (playerWon) ? 1 : 0;
        
        if(totalNumQTE > 0)
        {
            generateNewEvent();
        }
        else
        {
            gameObject.transform.parent.gameObject.GetComponent<SpawnerScript>().childDied(gameObject);
            puller.Besieged(this, false);
        }
    }

    public void generateNewEvent()
    {
        //currentEvent = null;
        //int number = UnityEngine.Random.Range(0, typeOfQTE.GetNames(typeof(typeOfQTE)).Length);
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
