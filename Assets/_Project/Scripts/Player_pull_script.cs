using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_pull_script : MonoBehaviour
{

    public Plant currentPlant;
    public float rotationSpeed = 50.0f;

    private float distanceFromPlant;
    private Vector3 trowelStartLocation;

    private bool SoilNeedsLoosened = true;

    //makes sure player actually covers all 360 degrees of the plant even if they backtrack
    private float leftMost { get; set; }
    private float rightMost { get; set; }
    public float currentAngle { get; set; }
    public float startingAngle { get; set; }

    private bool click = false;
    private bool hold = false;
    private bool pullingOut = false;

    private Vector3 lastPos;
    //public int lineDurration = 10000;
    //public float timerInterval = 0.05f;
    public float travelDistanceDraw = 0.5f;
    private Vector3 startPosition;
    public float distFromPlantToDraw = 1.0f;

    private Vector3 mouseStartPos;
    private float oldDistance = 0.0f;
    public float timeInPlace { get; set; }
    public float standstillTolerance = 0.5f;
    //public AnimationCurve pullJitterAmount;
    //public float jitterMult = 2.0f;

    void SetPlant(Plant plant)
    {
        currentPlant = plant;
        if (plant)
        {
            distanceFromPlant = (gameObject.transform.position - currentPlant.transform.position).magnitude;
            //currentPlant.stressMeter.HealthCap.fillAmount = currentPlant.diffiulty;
            //currentPlant.updatePullAngle();
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

    void OnApplicationQuit()
    {
        //running = false;
        //diggingHoleTimer.Stop();
    }

    // Start is called before the first frame update
    void Start()
    {
        SetPlant(currentPlant);
        if (SoilNeedsLoosened)
        {
            //diggingHoleTimer = new Timer();
            //diggingHoleTimer.Elapsed += new ElapsedEventHandler(onTimer);
            //diggingHoleTimer.Interval = timerInterval;

            lastPos = gameObject.transform.position;

            //diggingHoleTimer.Start();
            StartCoroutine(diggingHoleTimer());
        }
    }

    public IEnumerator diggingHoleTimer()
    {
        while (SoilNeedsLoosened)
        {
            //Debug.Log("hello?");
            Vector3 newPos = gameObject.transform.position;
            Vector3 directional = (currentPlant.transform.position - gameObject.transform.position).normalized * distFromPlantToDraw;

            float dist = (lastPos - newPos).magnitude;

            if (dist >= travelDistanceDraw)
            {
                Debug.DrawLine(lastPos + directional, newPos + directional, Color.red, 5, false);
                lastPos = newPos;
            }
            yield return new WaitForEndOfFrame();
            //yield return new WaitForSeconds(timerInterval);
        }
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
                    //Debug.Log("holdin");
                    hold = true;
                    mouseStartPos = Input.mousePosition;

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
                gameObject.transform.RotateAround(currentPlant.transform.position, Vector3.up, -rotationSpeed * Time.deltaTime);
                currentAngle -= rotationSpeed * Time.deltaTime;
                if (currentAngle < leftMost)
                    leftMost = currentAngle;
            }


            if (Input.GetButton("Clockwise"))
            {
                //Debug.Log("cw");
                gameObject.transform.RotateAround(currentPlant.transform.position, Vector3.up, rotationSpeed * Time.deltaTime);
                currentAngle += rotationSpeed * Time.deltaTime;
                if (currentAngle > rightMost)
                    rightMost = currentAngle;
            }

            currentPlant.UpdatePlayerLoation((startingAngle + currentAngle)%360);

            if (SoilNeedsLoosened)
            {
                if (Mathf.Abs(leftMost) + Mathf.Abs(rightMost) >= 360)
                {
                    //Debug.Log("all the way around");
                    //Debug.Log(leftMost);
                    //Debug.Log(rightMost);

                    SoilNeedsLoosened = false;

                    StopCoroutine(diggingHoleTimer());

                    currentPlant.loosenedPlant();
                    pullingOut = true;
                }
            }

            if (pullingOut)
            {
                //Input.mousePosition

                //not just raw distance.. we want distance from plant?
                //project line (mousepos - oldmousepos) onto the line that comes off the plant @ its given angle
                //start w/ Vector3.forward for the plant, rotate about y axis by the degrees given

                float distanceAway = 0;
                Vector3 projectOnto = currentPlant.projectOnto.normalized;
                Vector3 mouse = Input.mousePosition - mouseStartPos;

                if (hold)
                {
                    //Debug.Log("yo");
                    distanceAway = Vector3.Project(mouse, projectOnto).magnitude;
                }

                if (Mathf.Abs(oldDistance - distanceAway) <= standstillTolerance)
                {
                    timeInPlace += Time.deltaTime;
                    /*
                    float offput = jitterMult * pullJitterAmount.Evaluate(timeInPlace) / pullJitterAmount[pullJitterAmount.length - 1].value;
                    
                    Vector3 tangent;
                    Vector3 t1 = Vector3.Cross(projectOnto, currentPlant.transform.forward);
                    Vector3 t2 = Vector3.Cross(projectOnto, currentPlant.transform.up);
                    if (t1.magnitude > t2.magnitude)
                    {
                        tangent = t1;
                    }
                    else
                    {
                        tangent = t2;
                    }
                    float mouseDist = mouse.magnitude;
                    mouse += tangent * offput;
                    mouse = mouse.normalized * mouseDist;
                    distanceAway = Vector3.Project(mouse, projectOnto).magnitude;
                    */
                    
                }
                else
                {
                    //timeInPlace = 0.0f;
                    timeInPlace -= Time.deltaTime;
                    timeInPlace = Mathf.Max(timeInPlace, 0.0f);
                }

                
                Debug.Log(distanceAway);

                //Debug.Log("player pull!");
                //send command to plant!
                currentPlant.isPulling(hold, distanceAway);
                oldDistance = distanceAway;
            }
        }
    }
}
