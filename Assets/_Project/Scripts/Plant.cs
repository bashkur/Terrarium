using UnityEngine;

public class Plant : MonoBehaviour
{

    //the part of the circle around the plant the player need to pull in
    private float pullAngle = 0;

    //radius of interact circle around plant
    public float distanceFromPlant = 0;


    //for circle:
    /*
     * easy way is to have a thin circle around the plant w/ a point on it indicating hand direction
     * then have a hidden second circle that thickens to a point that we rotate so that the thickest
     * point is at the pullAngle and then hide it
     * we only reveal and part of it near player indicator
     * 
     * only issue is if we want to reuse this for say roots.... then we would have a variable number of 
     * bulges inidcating areas of interest... 
     * 
     * first things first get a circle + mouse indicator
     */

    private void Start()
    {
        pullAngle = Random.Range(0.0f, 360.0f);
    }

    private void Update()
    {
        // spawn a plant on left click
        if (Input.GetMouseButtonDown(0))
        {
            //Debug.Log("Plant a plant! :)");
        }
    }

    //draw glowing circle around plant that bulges out at the pull angle...
}
