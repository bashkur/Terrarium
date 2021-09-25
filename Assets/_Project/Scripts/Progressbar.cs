using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Progressbar : MonoBehaviour
{

    public Image fillBar;
    private RoundedEdge edge;
    public bool useRoundedEdge = true;

    void OnEnable()
    {

        fillBar = gameObject.GetComponent<Image>();

        if (edge == null)
        {
            useRoundedEdge = false;
        }
        else
        {
            edge.fillBar = fillBar;
        }
    }

    public void setFill(float amount)
    {
        fillBar.fillAmount = amount;
        if(useRoundedEdge)
        {
            edge.updateFill();
        }
    }
}
