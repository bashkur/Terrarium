using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandMovement : MonoBehaviour
{
    public float speed;

    private float mouseX = 0;
    private float mouseY = 0;

    private Camera mainCamera;
    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        mouseY = Input.GetAxis("Mouse Y");
        mouseX = Input.GetAxis("Mouse X");
        if(Mathf.Abs(mouseX) < 10 && Mathf.Abs(mouseY) < 10)
            transform.position = transform.position + new Vector3(mouseX * Time.deltaTime * speed , 0, mouseY * Time.deltaTime * speed);


    }

    private void OnTriggerStay(Collider other)
    {
        if (Input.GetAxis("Fire1") > 0)
        {
            Vector3 interactablesPos = other.gameObject.transform.position;

            StartCoroutine(moveCamera(interactablesPos));
            other.gameObject.GetComponent<IInteractable>().Interact();

        }
    }

    IEnumerator moveCamera(Vector3 pos)
    {
        Vector3 startingPos = mainCamera.transform.position;
        Vector3 targetPos = pos + (mainCamera.transform.position * 0.3f);
        for (float dist = 0f; dist < 1f; dist += 0.5f *Time.deltaTime)
        {

            Vector3 newPos = Vector3.Lerp(startingPos, targetPos, dist);
            newPos = new Vector3(newPos.x, startingPos.y, newPos.z);
            mainCamera.transform.position = newPos;
            mainCamera.transform.LookAt(pos);
            yield return new WaitForEndOfFrame();
        }
        GameObject T = GameObject.FindGameObjectWithTag("terrain");
        TerrainScript Tscript = T.GetComponent<TerrainScript>();
        StartCoroutine(Tscript.makeHole((int)pos.x + 10, (int)pos.z + 6));


    }
}
