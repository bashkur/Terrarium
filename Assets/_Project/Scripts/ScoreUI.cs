using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    private Animation animation;

    private void Awake()
    {
        animation = GetComponent<Animation>();
    }

    private void Start()
    {
        GameManager.Instance.OnScoreChanged += HandleScoreChanged;
        animation.Play();
    }

    private void HandleScoreChanged(int newval)
    {
        scoreText.SetText(newval.ToString());
    }
}
