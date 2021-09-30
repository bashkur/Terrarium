using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecalPlacement : MonoBehaviour
{

    public Texture decal;

    public bool placeMe = true;
    //public Vector2Int resolution = new Vector2Int(1024, 1024);
    public RenderTexture renderTexture;

    void OnCollisionEnter(Collision other)
    {
        if (placeMe)
        {
            placeMe = false;

            Material dest = other.gameObject.GetComponent<Renderer>().material;
            Texture destTexture = dest.GetTexture("_NormalTex");

            Vector2 offset = Vector2.zero;

            Graphics.Blit(destTexture, renderTexture, Vector3.one, Vector3.zero);

            //offset = new Vector2(other.contacts[0].point.x, other.contacts[0].point.z);

            Graphics.Blit(decal, renderTexture, Vector3.one, offset);

            destTexture = renderTexture;

            dest.SetTexture("_NormalTex", destTexture);
        }
    }

    void Start()
    {
        
    }
    
    void Update()
    {
        
    }
}
