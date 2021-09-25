using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Progressbar : MonoBehaviour
{

    private Image fillBar;
    public RoundedEdge edge;
    private bool enabled = false;

    void OnEnable()
    {

        fillBar = gameObject.GetComponent<Image>();

        edge.fillBar = fillBar;
        enabled = true;

    }

    public void setFill(float amount)
    {
        if (enabled)
        {
            fillBar.fillAmount = amount;
            edge.updateFill();
        }
    }
}
