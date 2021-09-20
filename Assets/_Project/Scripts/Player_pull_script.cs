using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_pull_script : MonoBehaviour
{

    public GameObject currentPlant;
    public float speed = 50.0f;

    private float distanceFromPlant;
    private Vector3 trowelStartLocation;

    private bool SoilNeedsLoosened = true;
    private float leftMost = 0;
    private float rightMost = 0;
    private float totalDegree = 0;

    void SetPlant(GameObject plant)
    {
        currentPlant = plant;
        if (plant)
        {
            distanceFromPlant = (gameObject.transform.position - currentPlant.transform.position).magnitude;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        SetPlant(currentPlant);
        if (SoilNeedsLoosened)
        {
            //rotates by -90 degrees
            gameObject.transform.localRotation = Quaternion.Euler(0,0,-90); //sets rotation
            //gameObject.transform.Rotate(0, 0, -90);

            //grab trowel and set up
        }
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
                totalDegree -= speed * Time.deltaTime;
                if (totalDegree < leftMost)
                    leftMost = totalDegree;
            }


            if (Input.GetButton("Clockwise"))
            {
                //Debug.Log("cw");
                gameObject.transform.RotateAround(currentPlant.transform.position, Vector3.up, speed * Time.deltaTime);
                totalDegree += speed * Time.deltaTime;
                if (totalDegree > rightMost)
                    rightMost = totalDegree;
            }

            if (SoilNeedsLoosened)
            {
                if (Mathf.Abs(leftMost) + Mathf.Abs(rightMost) >= 360)
                {
                    Debug.Log("all the way around");
                    Debug.Log(leftMost);
                    Debug.Log(rightMost);

                    SoilNeedsLoosened = false;
                }
            }
        }
    }
}
