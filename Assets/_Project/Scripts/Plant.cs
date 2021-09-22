using UnityEngine;
//using System;

public class Plant : MonoBehaviour
{

    //the part of the circle around the plant the player need to pull in
    public float pullAngle { get; set; }
    public float pullWeight { get; set; }
    public float playerLoation { get; set; }
    public float angleDifference { get; set; }
    public Vector3 playerStartPosition { get; set; }
    public GameObject debugArrow;
    public Vector3 projectOnto { get; set; }
    public float pullDistance;

    public Gradient IndicatorColors;
    //public GameObject stressMeterObj;
    
    public float currentPullTime { get; set; }
    public float pullTime = 1.0f;
    public float pullTolerance = 0.2f;
    public float diffiulty = 0.2f;
    public float numToComplete = 3;
    public Fillamount stressMeter;

    private bool pulling;
    private bool donePulling;

    private float totalTime = 0.0f;

    public void updatePullAngle()
    {
        //Debug.Log("setup");
        //also generates a random pullWeight
        pullAngle = UnityEngine.Random.Range(0.0f, 360.0f);
        stressMeter.HealthCap.fillAmount = diffiulty;
        pullWeight = UnityEngine.Random.Range(0.0f, 1.0f - diffiulty);
    }

    public void UpdatePlayerLoation(float currentAngle)
    {
        playerLoation = currentAngle;
        angleDifference = (playerLoation - pullAngle + 180 ) % 360 - 180;
        angleDifference = Mathf.Abs(angleDifference < -180 ? angleDifference + 360 : angleDifference);

        //Debug.Log(angleDifference);

        //point arrow at player
        //playerStartPosition.y = gameObject.transform.position.y;
        //Quaternion rotation = Quaternion.LookRotation(playerStartPosition, Vector3.up);
        //debugArrow.transform.rotation = rotation;
        //debugArrow.transform.Rotate(0, playerLoation, 0);
        debugArrow.transform.localEulerAngles = new Vector3(0, playerLoation, 0);
    }

    public void loosenedPlant()
    {
        //called to transition to next step in freeing the plant!
        //GetComponentInChildren
        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            GameObject child = gameObject.transform.GetChild(i).gameObject;
            if (child.GetComponent<ParticleGlowRing>())
            {
                //enabled = true/false; is for components only
                child.SetActive(true);
            }
        }
    }

    public void isPulling(bool isPull, float distance)
    {
        bool oldValue = pulling;

        pulling = isPull;

        //takes distance into account not just holding click
        if (pulling)
        {
            
            float dist = distance / pullDistance;
            //Debug.LogFormat("Perctent fill {0}, dist {1}", dist, distance);
            stressMeter.lerpFill(dist);
        }
        else
        {
            stressMeter.lerpFill(0.0f);
        }

        
        //increase stress the further you pull + lower maxhealth for stress if theres any uncut roots
    }

    private void Start()
    {

        //stressMeter = stressMeterObj.GetComponent<Fillamount>();

        Quaternion rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        debugArrow.transform.rotation = rotation;

        updatePullAngle();
        pulling = false;
        //pullAngle = 0;

        projectOnto = Quaternion.AngleAxis(pullAngle, Vector3.up) * Vector3.forward;
    }

    private void Update()
    {
        totalTime += Time.deltaTime;
        // spawn a plant on left click
        if (Input.GetMouseButtonDown(0))
        {
            //Debug.Log("Plant a plant! :)");
        }

        if (!donePulling && pulling)
        {
            if (Mathf.Abs(stressMeter.StressBar.fillAmount - pullWeight) <= pullTolerance)
            {
                currentPullTime += Time.deltaTime;
                if (currentPullTime >= pullTime)
                {
                    numToComplete--;
                    stressMeter.StressBar.fillAmount = 0.0f;
                    if (numToComplete > 0)
                    {
                        currentPullTime = 0;
                        updatePullAngle();
                    }
                    else
                    {
                        donePulling = true;
                        pulling = false;
                    }
                }
            }
            else if (currentPullTime > 0)
            {
                currentPullTime = Mathf.Max(currentPullTime - Time.deltaTime, 0.0f);
            }
        }
        else
        {
            //give player a score! they just pulled oout the plant
        }
    }

    //draw glowing circle around plant that bulges out at the pull angle...
}
