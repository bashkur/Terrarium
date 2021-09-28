using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class MaterialEditor : MonoBehaviour
{
    public SkinnedMeshRenderer renderer;
    public Vector3 offsetAmount;
    public bool customMin = false, customMax = false;
    public float minHeight, maxHeight;

    private Material material;
    private Vector3 initalPosition;
    private Vector3 oldPos;

    void OnEnable()
    {
        Start();
    }

    // Start is called before the first frame update
    void Start()
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
        oldPos = renderer.bounds.center;
        initalPosition = oldPos;

        updateMaterial();
    }

    void updateMaterial()
    {
        material.SetVector("_Offset", offsetAmount);

        //material.SetFloat("_MinHeight", renderer.bounds.center.y - renderer.bounds.size.y/2);
        //material.SetFloat("_MaxHeight", renderer.bounds.center.y + renderer.bounds.size.y/2);
        if (customMin)
        {
            material.SetFloat("_MinHeight", minHeight);
        }
        else
        {
            material.SetFloat("_MinHeight", -renderer.bounds.size.y / 2);
        }

        if(customMax)
        {
            material.SetFloat("_MaxHeight", maxHeight);
        }
        else
        {
            material.SetFloat("_MaxHeight", renderer.bounds.size.y / 2);
        }

        material.SetVector("_MeshWorldPos", renderer.bounds.center);

        oldPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (oldPos != renderer.bounds.center)
        {
            updateMaterial();
        }
    }
}
