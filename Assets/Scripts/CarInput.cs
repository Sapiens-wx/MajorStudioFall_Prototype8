using UnityEngine;
using System;
public class CarInput : Singleton<CarInput>
{
    [HideInInspector][NonSerialized] public Vector2 gearBoxInput, gearBoxInputDelta;
    [HideInInspector][NonSerialized] public float throttleInput;
    [HideInInspector][NonSerialized] public float brakeInput;
    [HideInInspector][NonSerialized] public float clutchInput;
    [HideInInspector][NonSerialized] public float steerInput;
    void FixedUpdate(){
        // Gear input
        gearBoxInputDelta=gearBoxInput;
        gearBoxInput.x=Input.GetAxis("GearX");
        gearBoxInput.y=Input.GetAxis("GearY");
        gearBoxInputDelta=gearBoxInput-gearBoxInputDelta;
        // paddle input
        if (Input.GetKey(KeyCode.Joystick1Button5)) {
            throttleInput=0;
            brakeInput=MathUtil.Map01(Input.GetAxis("Throttle"),-1f,1f);
        }
        else {
            throttleInput=MathUtil.Map01(Input.GetAxis("Throttle"),-1f,1f);
            brakeInput=0;
        }
        clutchInput=1-MathUtil.Map01(Input.GetAxis("Clutch"),-1f,1f);
        // steering
        steerInput=Input.GetAxis("Horizontal");
    }
}