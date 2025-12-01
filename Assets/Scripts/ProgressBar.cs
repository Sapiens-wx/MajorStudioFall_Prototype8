using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{
    public Image bar;

    [Range(0f, 1f)]
    [SerializeField] private float progress;

    [Header("Pedal Scaling")]
    public Transform pedal;

    public AnimationCurve scaleCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    private Vector3 pedalBaseScale;

    void Awake()
    {
        if (pedal != null)
        {
            pedalBaseScale = pedal.localScale;
        }
    }


    public void SetProgress(float value){
        progress = Mathf.Clamp01(value);

        if (bar != null)
        {
            bar.fillAmount = progress;
        }

        UpdatePedalScale(progress);
    }

    private void UpdatePedalScale(float t)
    {
        if (pedal == null) return;

        float factor = scaleCurve.Evaluate(t);
        pedal.localScale = pedalBaseScale * factor;
    }
}
