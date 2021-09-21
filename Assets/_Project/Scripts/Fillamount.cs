using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required when Using UI elements.

public class Fillamount : MonoBehaviour
{
    public Image StressBar;
    public Image HealthCap;
    private bool lerp = false;
    private float newTarget = 0.0f;
    public Gradient ColorChanger;
    public bool overStressed = false;

    void Start()
    {
        StressBar.fillAmount = 0;
    }

     public void lerpFill(float newAmmount)
     {
        lerp = true;
        newTarget = newAmmount;
     }

    // Update is called once per frame
    void Update()
    {
        if (lerp && StressBar.fillAmount >= newTarget)
        {
            lerp = false;
        }

        if (lerp == true)
        {
            StressBar.fillAmount = Mathf.Lerp(StressBar.fillAmount, newTarget, Time.deltaTime/2);
            StressBar.color = ColorChanger.Evaluate(StressBar.fillAmount / (1 - HealthCap.fillAmount));
        }

        if (StressBar.fillAmount < 1-HealthCap.fillAmount)
        {
            //StressBar.fillAmount += 1.0f / waitTime * Time.deltaTime;
        }
        else if (StressBar.fillAmount > 1-HealthCap.fillAmount)
        {
            overStressed = true;
            //Debug.Log("Over Stressed!");
        }
    }
}