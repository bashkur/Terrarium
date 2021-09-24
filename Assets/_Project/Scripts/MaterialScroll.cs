using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialScroll : MonoBehaviour
{
    public Material scrollingMat;

    [Range(0.0f, 1.0f)]
    public float uvSpeedX = 0.5f;
    [Range(0.0f, 1.0f)]
    public float uvSpeedY = 0.5f;

    public float lineThiness = 5.0f;
    //public float emission = 1.0f;
    [ColorUsage(true, true)]
    public Color col;
    
    // Start is called before the first frame update
    void Start()
    {
        scrollingMat.SetFloat("_UVScrollSpeedX", uvSpeedX);
        scrollingMat.SetFloat("_UVScrollSpeedY", uvSpeedY);
        scrollingMat.SetFloat("_LineThiness", lineThiness);
        //scrollingMat.SetFloat("_Emission", emission);

        scrollingMat.SetVector("_EmissionColor", new Vector3(col.r, col.g, col.b));
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
