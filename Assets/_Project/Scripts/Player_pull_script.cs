using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Timers;

public class Player_pull_script : MonoBehaviour
{

    public Plant currentPlant;
    public float speed = 50.0f;

    private float distanceFromPlant;
    private Vector3 trowelStartLocation;

    private bool SoilNeedsLoosened = true;

    //makes sure player actually covers all 360 degrees of the plant even if they backtrack
    private float leftMost = 0;
    private float rightMost = 0;
    public float currentAngle = 0;
    public float startingAngle = 0;

    private bool click = false;
    private bool hold = false;
    private bool pullingOut = false;


    private Timer diggingHoleTimer;
    private Vector3 lastPos;
    public int timerInterval = 250;
    public int lineDurration = 5;
    public float minDistanceBetweenCalls = 0.5f;
    //Debug.DrawLine (Vector3.zero, new Vector3 (1, 0, 0), Color.red);

    private Vector3 startPosition;

    void SetPlant(Plant plant)
    {
        currentPlant = plant;
        if (plant)
        {
            distanceFromPlant = (gameObject.transform.position - currentPlant.transform.position).magnitude;
        }
        if (SoilNeedsLoosened)
        {
            //rotates by -90 degrees
            startPosition = transform.position;
            currentPlant.playerStartPosition = startPosition;
            currentAngle = 0;

            gameObject.transform.localRotation = Quaternion.Euler(0, 0, -90);
            //rotates hand as a visual indicator that the player is loosening soil w/ trowel which isnt implemented yet  
            //grab trowel and set up

            //get angle b/w this gameobject location and plant.Forward

            Vector3 dir = (plant.gameObject.transform.position - gameObject.transform.position).normalized;
            //find angle = dot
            startingAngle = Mathf.Acos(Vector3.Dot(dir, plant.transform.forward));
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        SetPlant(currentPlant);
        if (SoilNeedsLoosened)
        {
            diggingHoleTimer = new Timer();
            diggingHoleTimer.Elapsed += new ElapsedEventHandler(onTimer);
            diggingHoleTimer.Interval = timerInterval;

            lastPos = gameObject.transform.position;

            diggingHoleTimer.Start();
        }
    }

    void onTimer(object source, ElapsedEventArgs e)
    {
        
        if (lastPos == null || (lastPos- gameObject.transform.position).magnitude < minDistanceBetweenCalls)
        {
            return;
        }

        Debug.Log("timer event");

        //do things
        Debug.DrawLine(lastPos, gameObject.transform.position, Color.red, lineDurration, false);
        lastPos = gameObject.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (currentPlant)
        {
            if (Input.GetButton("Fire1"))
            {
                if (!hold && click)
                {
                    Debug.Log("holdin");
                    hold = true;
                    click = false;
                }
                else if(!hold)
                {
                    click = true;
                }
            }
            if (Input.GetButtonUp("Fire1"))
            {
                click = false;
                hold = false;
            }

            //getButton does press + held, getButtonDown only goes off once when it was firt pressed
            if (Input.GetButton("Counterclockwise"))
            {
                //Debug.Log("ccw");
                gameObject.transform.RotateAround(currentPlant.transform.position, Vector3.up, -speed * Time.deltaTime);
                currentAngle -= speed * Time.deltaTime;
                if (currentAngle < leftMost)
                    leftMost = currentAngle;
            }


            if (Input.GetButton("Clockwise"))
            {
                //Debug.Log("cw");
                gameObject.transform.RotateAround(currentPlant.transform.position, Vector3.up, speed * Time.deltaTime);
                currentAngle += speed * Time.deltaTime;
                if (currentAngle > rightMost)
                    rightMost = currentAngle;
            }

            currentPlant.UpdatePlayerLoation((startingAngle + currentAngle)%360);

            if (SoilNeedsLoosened)
            {
                if (Mathf.Abs(leftMost) + Mathf.Abs(rightMost) >= 360)
                {
                    Debug.Log("all the way around");
                    //Debug.Log(leftMost);
                    //Debug.Log(rightMost);

                    SoilNeedsLoosened = false;
                    diggingHoleTimer.Stop();
                    currentPlant.loosenedPlant();
                    pullingOut = true;
                }
            }

            if (pullingOut)
            {
                //Debug.Log("pull!");
                //send command to plant!
                currentPlant.isPulling(hold);
            }
        }
    }
}
