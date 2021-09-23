using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IsVisibleOnCamera : MonoBehaviour
{
    private Renderer objectRenderer;
    private bool seen = false;
    private HandMovement parentObject;

    public bool getIsSeen()
    {
        return seen;
    }

    // Start is called before the first frame update
    void Start()
    {
        parentObject = gameObject.transform.parent.GetComponent("HandMovement") as HandMovement;
        objectRenderer = gameObject.GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update()
    {
        bool oldSeen = seen;

        seen = objectRenderer.isVisible;
        
        if(oldSeen != seen)
        {
            //send an update
            parentObject.updateVisibility(seen);
        }
    }
}
