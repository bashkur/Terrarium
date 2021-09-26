using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandMovement : MonoBehaviour
{
    public float speed;
    public float rotationSpeed;

    private float mouseX = 0;
    private float mouseY = 0;
    private bool stillVisible = true;

    private Camera mainCamera;
    private bool movingCamera = false;
    public float maxZoom;
    public float defaultZoom;

    private Player_pull_script puller;

    public Vector3 cameraOffset = new Vector3(0, 4, 0);
    public bool canMoveArms = true;

    public void DollyZoom(float amount, GameObject location)
    {
        //Camera.main.fieldOfView +- 2
        //orthographicSize
    }

    public void updateVisibility(bool childVisibleUpdate)
    {
        //this is called by child to update varriable instead of every frame...
        bool oldVisible = stillVisible;
        stillVisible = stillVisible && childVisibleUpdate;

        if (oldVisible != stillVisible)
        {
            if (!stillVisible)
            {
                movingCamera = true;

            }
        }
        
        if(stillVisible)
        {
            movingCamera = false;
        }

        //Debug.Log(stillVisible);
    }

    void rotateCameraTowardPosition(Vector3 pos)
    {
        Vector3 targetDirection = pos - mainCamera.transform.position;
        float singleStep = rotationSpeed * Time.deltaTime;
        Vector3 newLook = Vector3.RotateTowards(mainCamera.transform.forward, targetDirection, singleStep, 0.0f);
        mainCamera.transform.rotation = Quaternion.LookRotation(newLook);
    }

    void OnEnable()
    {
        mainCamera = Camera.main;
        mainCamera.depthTextureMode = DepthTextureMode.Depth;
    }

    void Start()
    {
        //mainCamera = Camera.main;
        Cursor.visible = false;
        puller = gameObject.GetComponent<Player_pull_script>();
    }

    // Update is called once per frame
    void Update()
    {

        if (movingCamera)
        {
            //rotateCameraTowardPosition(gameObject.transform.position);
            //rotates too much...
        }

        //Debug.Log(allOnScreen());
        if (canMoveArms)
        {
            mouseY = Input.GetAxis("Mouse Y"); // current movement input
            mouseX = Input.GetAxis("Mouse X");
            if (Mathf.Abs(mouseX) < 10 && Mathf.Abs(mouseY) < 10)
            {
                transform.position += new Vector3(mouseX * Time.deltaTime * speed, 0, mouseY * Time.deltaTime * speed); //should be negative.. moves away from mouse input?
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {

        //Debug.Log("overlapping");

        if (Input.GetAxis("Fire1") > 0 && !puller.currentPlant)
        {
            //Debug.Log("clicked on interactable obj");
            Vector3 interactablesPos = other.gameObject.transform.position;
            other.gameObject.GetComponent<IInteractable>()?.Interact();
            Plant plant = other.gameObject.GetComponent<Plant>();

            StartCoroutine(moveCamera(interactablesPos, interactablesPos + cameraOffset, interactablesPos));
           
            if (plant)
            {
                Debug.Log("clicked on plant!");
                puller.SetPlant(plant);
            }

        }
    }

    IEnumerator moveCamera(Vector3 pos, Vector3 moveTo, Vector3 lookAt)
    {
        Vector3 startingPos = mainCamera.transform.position;
        //Vector3 targetPos = pos - (absVector3(mainCamera.transform.position) * 0.4f);
        Vector3 targetPos = moveTo - (absVector3(mainCamera.transform.position) * 0.4f);

        float totalDist = (targetPos- startingPos).magnitude;

        for (float dist = 0f; dist < 1f; dist += 0.5f *Time.deltaTime)
        {

            Vector3 newPos = Vector3.Lerp(startingPos, targetPos, dist);
            newPos = new Vector3(newPos.x, startingPos.y, newPos.z);
            mainCamera.transform.position = newPos;
            mainCamera.transform.LookAt(lookAt);

            float temp = (targetPos - mainCamera.transform.position).magnitude;
            dist = temp / totalDist;

            yield return new WaitForEndOfFrame();
        }

        DigHere(pos);
        //GameObject T = GameObject.FindGameObjectWithTag("terrain");
        //TerrainScript Tscript = T.GetComponent<TerrainScript>();
        //StartCoroutine(Tscript.makeHole((int)pos.x , (int)pos.z));//for testing hole making

        //mainCamera.transform.eulerAngles = new Vector3(mainCamera.transform.eulerAngles.x, 90, mainCamera.transform.eulerAngles.z);
        mainCamera.transform.LookAt(lookAt);
    }

    public void DigHere(Vector3 pos)
    {
        GameObject T = GameObject.FindGameObjectWithTag("terrain");
        TerrainScript Tscript = T.GetComponent<TerrainScript>();
        StartCoroutine(Tscript.makeHole((int)pos.x, (int)pos.z));
    }

    private Vector3 absVector3(Vector3 V)
    {
        return new Vector3(Mathf.Abs(V.x), Mathf.Abs(V.y), Mathf.Abs(V.z));
    }
}
