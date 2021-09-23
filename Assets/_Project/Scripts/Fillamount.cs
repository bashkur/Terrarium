using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required when Using UI elements.
using System;

public class Fillamount : MonoBehaviour
{
    public Image StressBar;
    public Image HealthCap;

    public GameObject arrowContainer;
    public GameObject topArrow { get; set; }
    public GameObject bottomArrow { get; set; }

    private bool lerp = false;
    private float newTarget = 0.0f;
    public Gradient ColorChanger;
    public bool overStressed { get; set; }
    private bool positiveDirection = true;
    private float arrow_y1 = 0.0f, arrow_y2 = 0.0f;

    //public event EventHandler stressChangedEvent;

    void Start()
    {
        overStressed = false;
        StressBar.fillAmount = 0;
        //lerpFill(0.0f);

        topArrow = arrowContainer.transform.GetChild(0).gameObject;
        bottomArrow = arrowContainer.transform.GetChild(1).gameObject;
        setArrowPosition(arrow_y1, arrow_y2);
    }

    public void setArrowPosition(float y1, float y2)
    {
        arrow_y1 = y1 * (272 + 172) - 172;
        arrow_y2 = y2 * (272 + 172) - 172;

        //-172 -> 272
        if (topArrow)
        {
            topArrow.transform.position = new Vector3(0, y1, 0);
        }
        if (bottomArrow)
        {
            bottomArrow.transform.position = new Vector3(0, y2, 0);
        }
    }

    public void lerpFill(float newAmmount)
     {
        //Debug.Log("lerp");
        lerp = true;
        positiveDirection = newAmmount > StressBar.fillAmount;
        newTarget = newAmmount;
     }

    // Update is called once per frame
    void Update()
    {
        if (lerp && (positiveDirection && StressBar.fillAmount >= newTarget || !positiveDirection && StressBar.fillAmount <= newTarget))
        {
            lerp = false;
        }

        if (lerp == true)
        {
            StressBar.fillAmount = Mathf.Lerp(StressBar.fillAmount, newTarget, Time.deltaTime/2);
            StressBar.color = ColorChanger.Evaluate(StressBar.fillAmount / (1 - HealthCap.fillAmount));
        }

        if (StressBar.fillAmount > 1-HealthCap.fillAmount)
        {
            overStressed = true;
            //Debug.Log("Over Stressed!");
        }
    }
}
