using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

//disables warnings for color and size.. they still work
#pragma warning disable 0618

//0, 360
//.12, 57.94232
//.49, 8.272974
//0,0

public class ParticleGlowRing : MonoBehaviour
{

    private ParticleSystem circleIndicator;
    private ParticleSystem.Particle[] particles;

    //public GameObject attractedTo;
    //private float maxDist = 5.0f;

    private Plant parentPlant;

    public AnimationCurve curve;

    void Start()
    {
        InitializeIfNeeded();
        parentPlant = gameObject.transform.parent.GetComponent<Plant>();
        //AnimationClip clip = gameObject.transform.parent.GetComponent<Animation>().clip;
        //AnimationClipCurveData[] allCurves = GetAllCurves(clip, true);
        //curve = allCurves[0].curve;
    }

    /*
    //yoinked and edited 8)
    public static AnimationClipCurveData[] GetAllCurves(AnimationClip clip, bool includeCurveData)
    {
        EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(clip);
        AnimationClipCurveData[] dataArray = new AnimationClipCurveData[curveBindings.Length];
        for (int i = 0; i < dataArray.Length; i++)
        {
            dataArray[i] = new AnimationClipCurveData(curveBindings[i]);
            if (includeCurveData)
            {
                dataArray[i].curve = AnimationUtility.GetEditorCurve(clip, curveBindings[i]);
            }
        }
        return dataArray;
    }
    */

    void InitializeIfNeeded()
    {
        if (circleIndicator == null)
            circleIndicator = GetComponent<ParticleSystem>();

        if (particles == null || particles.Length < circleIndicator.main.maxParticles)
            particles = new ParticleSystem.Particle[circleIndicator.main.maxParticles];
    }



    void LateUpdate()
    {
        InitializeIfNeeded();

        int numParticlesAlive = circleIndicator.GetParticles(particles);

        float newSize = 0.0001f;
        //((parentPlant.getCurrentDifference()) / 360.0f) = (0.0f, 1.0f]
        //newSize = (1-(parentPlant.getCurrentDifference() / 360.0f)) * (0.2f - 0.0001f) + 0.0001f;
        newSize = curve.Evaluate(((parentPlant.getCurrentDifference() / 360.0f))) / 360.0f * (0.2f - 0.0001f) + 0.0001f;

        Color newColor = parentPlant.IndicatorColors.Evaluate(1 - (parentPlant.getCurrentDifference() / 360.0f));

        for (int i = 0; i < numParticlesAlive; i++)
        {
            //particles[i].size = Mathf.Lerp(particles[i].size, newSize, Time.deltaTime);
            particles[i].size = newSize;
            particles[i].color = newColor;
            //particles[i].size = aniamtionCuveItem;
        }
        circleIndicator.SetParticles(particles, numParticlesAlive);
    }

}
