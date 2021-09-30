using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteAlways]
public class DecalPlacement : MonoBehaviour
{

    public Texture decal;

    public bool placeMe = true;
    //public Vector2Int resolution = new Vector2Int(1024, 1024);
    public RenderTexture renderTexture;
    
    private Vector2 offset = Vector2.zero;
    public float bounce = 100.0f;

    void OnCollisionEnter(Collision other)
    {
        //if (placeMe)
        {

            Material dest = other.gameObject.GetComponent<Renderer>().material;
            Texture destTexture = dest.GetTexture("_NormalTex");

            Graphics.Blit(destTexture, renderTexture, Vector3.one, Vector3.zero);
            
            if (placeMe)
            {
                RaycastHit hit = new RaycastHit();
                //Ray ray = new Ray(other.contacts[0].point - other.contacts[0].normal, other.contacts[0].normal);
                Ray ray = new Ray(gameObject.transform.position, (other.contacts[0].point - gameObject.transform.position).normalized );

                Debug.DrawRay(ray.origin, ray.direction, Color.green, 100, false);

                if (Physics.Raycast(ray, out hit))
                {
                    Debug.DrawRay(hit.point, hit.normal, Color.red, 100, false);

                    offset = hit.textureCoord;
                    offset.x *= destTexture.width;
                    offset.y *= destTexture.height;

                    //dDebug.LogFormat("offset: {0}, hitPoint: {1}, UV: {2}", offset, hit.point, destTexture.height, hit.textureCoord);
                }
            }
            Graphics.Blit(decal, renderTexture, Vector3.one, offset);

            destTexture = renderTexture;

            dest.SetTexture("_NormalTex", destTexture);
            placeMe = false;
        }

        gameObject.GetComponent<Rigidbody>().AddForce(gameObject.transform.up  * bounce);
    }

    void Start()
    {

    }

    void Update()
    {

    }
}
