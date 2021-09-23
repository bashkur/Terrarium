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

    void Start()
    {
        mainCamera = Camera.main;
        Cursor.visible = false;
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
        mouseY = Input.GetAxis("Mouse Y"); // current movement input
        mouseX = Input.GetAxis("Mouse X");
        if (Mathf.Abs(mouseX) < 10 && Mathf.Abs(mouseY) < 10)
        {
            transform.position += new Vector3(mouseX * Time.deltaTime * speed, 0, mouseY * Time.deltaTime * speed); //should be negative.. moves away from mouse input?
        }
    }

    private void OnTriggerStay(Collider other)
    {

        //Debug.Log("overlapping");

        if (Input.GetAxis("Fire1") > 0)
        {
            //Debug.Log("clicked on interactable obj");
            Vector3 interactablesPos = other.gameObject.transform.position;

            StartCoroutine(moveCamera(interactablesPos,interactablesPos-new Vector3(0,0.3f,0)));
            other.gameObject.GetComponent<IInteractable>().Interact();

        }
    }

    IEnumerator moveCamera(Vector3 pos,Vector3 lookAt)
    {
        Vector3 startingPos = mainCamera.transform.position;
        Vector3 targetPos = pos - (absVector3(mainCamera.transform.position) * 0.4f);
        for (float dist = 0f; dist < 1f; dist += 0.5f *Time.deltaTime)
        {

            Vector3 newPos = Vector3.Lerp(startingPos, targetPos, dist);
            newPos = new Vector3(newPos.x, startingPos.y, newPos.z);
            mainCamera.transform.position = newPos;
            mainCamera.transform.LookAt(lookAt);
            yield return new WaitForEndOfFrame();
        }
        GameObject T = GameObject.FindGameObjectWithTag("terrain");
        TerrainScript Tscript = T.GetComponent<TerrainScript>();
        StartCoroutine(Tscript.makeHole((int)pos.x + 10, (int)pos.z + 6)); //for testing hole making


    }

    private Vector3 absVector3(Vector3 V)
    {
        return new Vector3(Mathf.Abs(V.x), Mathf.Abs(V.y), Mathf.Abs(V.z));
    }
}
