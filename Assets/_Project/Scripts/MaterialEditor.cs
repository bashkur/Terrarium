using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class MaterialEditor : MonoBehaviour
{
    public SkinnedMeshRenderer renderer;
    public Vector4 initalOffset = new Vector4(0, 0.613f, 0, 0);
    public GameObject TopMostParent;

    private Material material;
    private Vector3 initalPosition;
    private Vector3 oldPos;

    private Vector3 initalScale;
    private Vector3 oldScale;
    // Start is called before the first frame update
    void OnStart()
    {
        if (material == null)
        {
            if (Application.isPlaying)
            {
                material = renderer.material;
            }
            else
            {
                material = renderer.sharedMaterial;
            }
        }

        //initalOffset = material.GetVector("_Offset");
        oldPos = TopMostParent.transform.position + transform.position;
        initalPosition = oldPos;

        //lossyScale
        oldScale = getScale();
        initalScale = oldScale;
        
        //material.SetVector("_Offset", (initalOffset - new Vector4(temp.x, temp.y, temp.z - initalPosition.z, 0)) * (oldScale/initalScale));
        updateMaterial();
    }

    void updateMaterial()
    {
        Vector3 temp = transform.position - initalPosition;
        Vector4 translationChange = (initalOffset - new Vector4(temp.x, temp.y, temp.z - initalPosition.z, 0));

        oldScale = getScale();

        //Debug.LogFormat("{0}, {1}, {2}", oldScale.x / initalScale.x, oldScale.y / initalScale.y, oldScale.z / initalScale.z);

        material.SetVector("_Offset", translationChange);

        material.SetVector("_Scale", new Vector4(oldScale.x / initalScale.x, oldScale.y / initalScale.y, oldScale.z / initalScale.z, 1));

        oldPos = TopMostParent.transform.position + transform.position;
    }

    public Vector3 getScale()
    {
        /*
        Vector3 scale = transform.localScale;
        GameObject current = transform.parent.gameObject;

        while(current != TopMostParent)
        {

            scale.x *= current.transform.localScale.x;
            scale.y *= current.transform.localScale.y;
            scale.z *= current.transform.localScale.z;

            current = transform.parent.gameObject;
        }

        Debug.Log(scale);
        return scale;
        */

        /*
        Vector3 scale = new Vector3(0,0,0);
        scale.x = TopMostParent.transform.localScale.x * transform.localScale.x;

        scale.x = TopMostParent.transform.localScale.y * transform.localScale.y;

        scale.x = TopMostParent.transform.localScale.z * transform.localScale.z;
        */

        //.bounds.size
        Vector3 size = new Vector3(1, 1, 1);
        size = multVector(transform.localScale, multVector(renderer.bounds.size, TopMostParent.transform.localScale));
        return size;
    }

    Vector3 multVector(Vector3 vec1, Vector3 vec2)
    {
        return new Vector3(vec1.x * vec2.x, vec1.y * vec2.y, vec1.z * vec2.z);
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(transform.lossyScale);
        Vector3 scale = getScale();
        if (oldPos != TopMostParent.transform.position + transform.position || oldScale != scale)
        {
            updateMaterial();

            //material.SetVector("_Offset", initalOffset - new Vector4(temp.x, temp.y, temp.z, 0));
        }
    }
}
