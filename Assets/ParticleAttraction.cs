using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleAttraction : MonoBehaviour
{

    public ParticleSystem circleIndicator;
    public ParticleSystem.Particle[] particles;

    public GameObject attractedTo;

    void Start()
    {
        circleIndicator = gameObject.GetComponent(ParticleSystem) as ParticleSystem;
        particles = circleIndicator.particles;
    }

    void Update()
    {
        for (int i = 0; i < particles.GetUpperBound(0); i++)
        {
            particles[i].position = Vector3.Lerp(particles[i].position, attractedTo.transform.position, Time.deltaTime / 2.0f);
        }
        circleIndicator.particles = particles;
    }

}
