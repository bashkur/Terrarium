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
    public float travelDistanceDraw = 0.5f;
    private Vector3 startPosition;
    public float distFromPlantToDraw = 1.0f;

    private Vector3 mouseStartPos;
    private float oldDistance = 0.0f;
    public float timeInPlace { get; set; }
    public float standstillTolerance = 0.5f;
    public AnimationCurve pullJitterAmount;
    public float jitterMult = 2.0f;

    private HandMovement ericScript;

    float calcAngle(GameObject objectOrbitting)
    {
        float angle = 0;
        Vector3 dir = (gameObject.transform.position - objectOrbitting.transform.position).normalized;
        
        Vector2 dir2 = new Vector2(dir.x, dir.z);
        Vector2 for2 = new Vector2(objectOrbitting.transform.forward.x, objectOrbitting.transform.forward.z);
        float dot = Vector3.Dot(for2, dir2);
        float cross = (dir2.x * for2.y - dir2.y * for2.x);
        angle = Mathf.Atan2(cross, dot);
        angle *= Mathf.Rad2Deg;
        
        //angle = Vector3.Angle(dir, transform.forward);

        return angle - 90;
    }

    void SetPlant(Plant plant)
    {
        currentPlant = plant;
        if (plant)
        {
            distanceFromPlant = (gameObject.transform.position - currentPlant.transform.position).magnitude;

        }
        if (SoilNeedsLoosened)
        {
            startPosition = transform.position;
            currentPlant.playerStartPosition = startPosition;
            currentAngle = 0;

            startingAngle = calcAngle(plant.gameObject);
            //Debug.Log(startingAngle);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        //body = GetComponent<Rigidbody>();
        ericScript = gameObject.GetComponent<HandMovement>();
        ericScript.enabled = true;

        SetPlant(currentPlant);
        if (SoilNeedsLoosened)
        {
            ericScript.enabled = false;
            lastPos = gameObject.transform.position;
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
                    //mouseStartPos = gameObject.transform.position;
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

                currentPlant.dontHold = false;
            }

            if (Input.GetButtonUp("Fire2"))
            {
                //resets position if hand gets too far away
                Vector3 dir = (gameObject.transform.position - currentPlant.transform.position).normalized;
                gameObject.transform.position = currentPlant.transform.position + dir * distanceFromPlant;
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

            if (ericScript.enabled == true)
            {
                currentAngle = calcAngle(currentPlant.gameObject);
                //Debug.Log(currentAngle);

                currentPlant.UpdatePlayerLoation((currentAngle));
            }
            else
            {
                currentPlant.UpdatePlayerLoation((startingAngle + currentAngle));
            }

            if (SoilNeedsLoosened)
            {
                if (Mathf.Abs(leftMost) + Mathf.Abs(rightMost) >= 360)
                {
                    //Debug.Log("all the way around");
                    //Debug.Log(leftMost);
                    //Debug.Log(rightMost);

                    SoilNeedsLoosened = false;

                    StopCoroutine(diggingHoleTimer());

                    currentPlant.turnOnParticleEffectRing();
                    pullingOut = true;

                    ericScript.enabled = true;
                }
            }

            if (pullingOut && !currentPlant.dontHold)
            {
                float distanceAway = 0;
                Vector3 projectOnto = currentPlant.projectOnto.normalized;
                //Vector3 mouse = Input.mousePosition - mouseStartPos;
                Vector3 mouse = gameObject.transform.position - currentPlant.transform.position;

                if (hold)
                {
                    //distanceAway = Vector3.Project(mouse, projectOnto).magnitude;
                    distanceAway = mouse.magnitude;
                }

                if (hold && Mathf.Abs(oldDistance - distanceAway) <= standstillTolerance)
                {
                    timeInPlace += Time.deltaTime;

                    timeInPlace %= pullJitterAmount[pullJitterAmount.length - 1].time;

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
                    //body.AddForce(tangent * offput);

                    gameObject.transform.position += tangent * offput * Time.deltaTime;

                }
                else
                {
                    //timeInPlace = 0.0f;
                    timeInPlace -= Time.deltaTime;
                    timeInPlace = Mathf.Max(timeInPlace, 0.0f);
                }

                
                //Debug.Log(distanceAway);

                //Debug.Log("player pull!");
                //send command to plant!
                currentPlant.isPulling(hold, distanceAway);
                oldDistance = distanceAway;
            }
        }
    }
}
