using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        SetPlant(currentPlant);
    }

    // Update is called once per frame
    void Update()
    {
        if (currentPlant)
        {

            //getButton does press + held, getButtonDown only goes off once when it was firt pressed
            if (Input.GetButton("Counterclockwise"))
            {
                //Debug.Log("ccw");
                gameObject.transform.RotateAround(currentPlant.transform.position, Vector3.up, -speed * Time.deltaTime);
                currentAngle -= speed * Time.deltaTime;
                currentAngle %= 360;
                if (currentAngle < leftMost)
                    leftMost = currentAngle;
            }


            if (Input.GetButton("Clockwise"))
            {
                //Debug.Log("cw");
                gameObject.transform.RotateAround(currentPlant.transform.position, Vector3.up, speed * Time.deltaTime);
                currentAngle += speed * Time.deltaTime;
                currentAngle %= 360;
                if (currentAngle > rightMost)
                    rightMost = currentAngle;
            }

            currentPlant.UpdatePlayerLoation(currentAngle);


            if (SoilNeedsLoosened && Mathf.Abs(leftMost) + Mathf.Abs(rightMost) >= 360)
            {
                Debug.Log("all the way around");
                //Debug.Log(leftMost);
                //Debug.Log(rightMost);

                SoilNeedsLoosened = false;
                currentPlant.loosenedPlant();
            }
        }
    }
}
