using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Techometer : Singleton<Techometer>
{
    public float minSpeedArrowAngle;
    public float maxSpeedArrowAngle;

    [Header("UI")]
    public TMP_Text rpmText;
    public RectTransform arrow;

    private void Update()
    {
        float rpm = CarCtrl.inst.rpm;

        if (rpmText != null)
            rpmText.text = ((int)rpm).ToString();
        if (arrow != null) {
            arrow.localRotation=Quaternion.Lerp(arrow.localRotation,Quaternion.Euler(new Vector3(0, 0, Mathf.Lerp(minSpeedArrowAngle, maxSpeedArrowAngle, rpm / CarCtrl.inst.maxRpm))),.04f);
        }
    }
}