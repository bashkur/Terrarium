using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required when Using UI elements.
using System;

public class Fillamount : MonoBehaviour
{
    public Image StressBar;
    public Image HealthCap;

    public GameObject topArrow;
    public GameObject bottomArrow;

    private bool lerp = false;
    private float newTarget = 0.0f;
    public Gradient ColorChanger;
    public bool overStressed { get; set; }
    private bool positiveDirection = true;
    private float height;

    void Start()
    {
        overStressed = false;
        StressBar.fillAmount = 0;
        //lerpFill(0.0f);
        if (height <= 0)
        {
            height = StressBar.gameObject.transform.GetComponent<RectTransform>().rect.height;
            //Debug.Log(height);
        }
    }

    public void setMeAndTheBoisActive(bool active)
    {
        gameObject.transform.parent.gameObject.SetActive(active);
    }

    public void setArrowPosition(float target, float tolerance)
    {
        //float y1 = (target - tolerance) * (211 + 205) - 205;
        //float y2 = (target + tolerance) * (211 + 205) - 205;

        if (height <= 0)
        {
            height = StressBar.gameObject.transform.GetComponent<RectTransform>().rect.height;
        }
        
        float y1 = Mathf.Max((target + tolerance) * height, 0);
        float y2 = Mathf.Max((target - tolerance) * height, 0);

        //Debug.LogFormat("{0}, and {1}", y1, y2);

        //-205 -> 211
        RectTransform temp = topArrow.GetComponent<RectTransform>();
        temp.localPosition = new Vector3(temp.localPosition.x, y1, 0);

        temp = bottomArrow.GetComponent<RectTransform>();
        temp.localPosition = new Vector3(temp.localPosition.x, y2, 0);

        //topArrow.transform.position = new Vector3(topArrow.transform.position.x, y1, 0);
        //bottomArrow.transform.position = new Vector3(topArrow.transform.position.x, y2, 0);        
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
            StressBar.fillAmount = Mathf.Lerp(StressBar.fillAmount, newTarget, Time.deltaTime);
            StressBar.color = ColorChanger.Evaluate(StressBar.fillAmount / (1 - HealthCap.fillAmount));
        }

        if (StressBar.fillAmount > 1-HealthCap.fillAmount)
        {
            overStressed = true;
            //Debug.Log("Over Stressed!");
        }
    }
}
