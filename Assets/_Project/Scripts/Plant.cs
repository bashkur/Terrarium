using UnityEngine;

public class Plant : MonoBehaviour
{

    //the part of the circle around the plant the player need to pull in
    public float pullAngle = 0;
    public float playerLoation;
    public float angleDifference;
    public Vector3 playerStartPosition;

    public Gradient IndicatorColors;

    public float aniamtionCuveItem;

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

    public float getCurrentDifference()
    {
        return angleDifference;
    }

    public void UpdatePlayerLoation(float currentAngle)
    {
        playerLoation = currentAngle;
        angleDifference = (playerLoation - pullAngle + 180 ) % 360 - 180;
        angleDifference = Mathf.Abs(angleDifference < -180 ? angleDifference + 360 : angleDifference);

        //Debug.Log(angleDifference);
    }

    public void loosenedPlant()
    {
        //called to transition to next step in freeing the plant!
        //GetComponentInChildren
        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            GameObject child = gameObject.transform.GetChild(i).gameObject;
            if (child.GetComponent<ParticleAttraction>())
            {
                //enabled = true/false; is for components only
                child.SetActive(true);
            }
        }
    }

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
