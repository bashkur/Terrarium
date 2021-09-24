using UnityEngine;
//using System;

public class Plant : MonoBehaviour
{

    //the part of the circle around the plant the player need to pull in
    public float pullAngle { get; set; }
    public float pullWeight { get; set; }
    public float playerLoation { get; set; }
    public float angleDifference { get; set; }
    public float angleDifferenceWeight;
    public Vector3 playerStartPosition { get; set; }
    public GameObject debugArrow;
    public Vector3 projectOnto { get; set; }
    public float pullDistance;

    public Gradient IndicatorColors;
    //public GameObject stressMeterObj;
    
    public float currentPullTime { get; set; }
    public float pullTime = 1.0f;
    public float pullTolerance = 0.2f;
    private float diffiulty = 0.2f;
    public AnimationCurve difficultyCurve;
    public float numToComplete = 3;
    public Fillamount stressMeter;
    public float minPull = 0.1f;
    

    private bool pulling;
    private bool donePulling;

    private float totalTime = 0.0f;

    public void updatePullAngle()
    {
        diffiulty = difficultyCurve.Evaluate(numToComplete);
        //Debug.Log("setup");
        //also generates a random pullWeight
        pullAngle = UnityEngine.Random.Range(0.0f, 360.0f);
        stressMeter.HealthCap.fillAmount = diffiulty;
        pullWeight = UnityEngine.Random.Range(minPull, 1.0f - diffiulty);

        projectOnto = Quaternion.AngleAxis(pullAngle, Vector3.up) * Vector3.forward;

        //y = pullWeight +- pullTolerance
        //Debug.LogFormat("{0} + {1} = {2}, {0} - {1} = {3}", pullWeight, pullTolerance, pullWeight + pullTolerance, pullWeight - pullTolerance);
        stressMeter.setArrowPosition(pullWeight, pullTolerance);
        UpdatePlayerLoation(playerLoation);

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

        //currentPlayerLocale.y = debugArrow.transform.position.y;
        //Quaternion rotation = Quaternion.LookRotation(currentPlayerLocale, Vector3.up);
        //debugArrow.transform.rotation = rotation;
    }

    public void turnOnParticleEffectRing(bool setVal = true)
    {
        //called to transition to next step in freeing the plant!
        //GetComponentInChildren

        debugArrow.SetActive(setVal);

        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            GameObject child = gameObject.transform.GetChild(i).gameObject;
            if (child.GetComponent<ParticleGlowRing>())
            {
                //enabled = true/false; is for components only
                child.SetActive(setVal);
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
            //Debug.Log("pulln");
            
            float dist = distance / pullDistance;
            //Debug.LogFormat("Perctent fill {0}, dist {1}", dist, distance);

            dist *= angleDifference/angleDifferenceWeight;

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

        stressMeter.gameObject.SetActive(true);

    }

    private void Update()
    {
        totalTime += Time.deltaTime;
        // spawn a plant on left click
        if (Input.GetMouseButtonDown(0))
        {
            //Debug.Log("Plant a plant! :)");
        }

        if (!donePulling && pulling && numToComplete > 0)
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
                        turnOnParticleEffectRing(false);
                        stressMeter.gameObject.SetActive(false);
                        Debug.Log("done pulling");
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
