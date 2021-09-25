using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerScript : MonoBehaviour
{
    public GameObject thingToSpawn;
    public float interval;
    public int maxActive;
    public int startingNum;
    public float range;
    private int active;
    private float timeSinceSpawn;
    private float timeSinceDed;
    private List<GameObject> children;
    private TerrainScript tScript;
    void Start()
    {
        tScript = GameObject.FindGameObjectWithTag("terrain").GetComponent<TerrainScript>(); //dirty one line.... you know you like it.
        
        
        children = new List<GameObject>();
        active = 0;
        timeSinceSpawn = 0;
        timeSinceDed = 0;
    }
    
    void Update()
    {
        if(active == 0 || active < startingNum)
        {
            for (int i = startingNum; i > 0; --i)
                spawnObj();
        }
        if (timeSinceDed >= interval && active >= maxActive && Random.Range(0, 100) == 0)// random to add some spice!!
        {
            cullTheHerd();
        }
        if (timeSinceSpawn >= interval && active < maxActive && Random.Range(0,100) == 0)// random to add some spice!!
        {
            spawnObj();
        }
        

        timeSinceDed += Time.deltaTime;
        timeSinceSpawn += Time.deltaTime;

    }

    void spawnObj()
    {

        Vector3 pos = transform.position + spawnCords();
        children.Add(Instantiate(thingToSpawn, pos, transform.rotation, transform));
        StartCoroutine(tScript.BulgeMe((int)pos.x, (int)pos.z ));
        active++;
        timeSinceSpawn = 0;
    }
    //probably a better way to do this but meh
    Vector3 spawnCords()
    {
        int interations = 0;//need to just return if we go too long
        Vector3 retVal = new Vector3(0, 0, 0);
        float minDist = -1;
        while(minDist <= range / (maxActive/5) && interations < 100)
        {
            interations += 1;
            retVal = new Vector3(Random.Range(1f, range), -0.2f, Random.Range(1f, range));
            foreach (Transform child in transform)
            {
                float dist = Mathf.Abs(Vector3.Distance(retVal, child.position));
                if (minDist > dist)
                    minDist = dist;

            }
        }
        return retVal;
        

    }
    void cullTheHerd() //sacrifice is sometimes necessary 
    {
        foreach (Transform child in transform)//Culling
        {
            if (Random.Range(0, active / 3) == 0)
            {
                childDied(child.gameObject);
                Destroy(child.gameObject);
                
            }
        }

    }
    public void childDied(GameObject child)
    {
        children.Remove(child);
        active--;
        timeSinceDed = 0;
    }

    private Vector3 absVector3(Vector3 V)
    {
        return new Vector3(Mathf.Abs(V.x), Mathf.Abs(V.y), Mathf.Abs(V.z));
    }
}
