using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestInteractable : MonoBehaviour,IInteractable
{
    // Start is called before the first frame update
    void Start()
    {
        
    }
    void IInteractable.Interact()
    {
        //print("YAY");
        //transform.GetComponent<MeshRenderer>().enabled = false;
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
