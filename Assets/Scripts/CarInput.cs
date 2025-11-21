using UnityEngine;
using System;
using VehiclePhysics;

public class CarInput : Singleton<CarInput>
{
    [HideInInspector][NonSerialized] public Vector2 gearBoxInput;
    [HideInInspector][NonSerialized] public float throttleInput;
    [HideInInspector][NonSerialized] public float brakeInput;
    [HideInInspector][NonSerialized] public float clutchInput;
    [HideInInspector][NonSerialized] public float steerInput;
    void FixedUpdate(){
        // Gear input
        gearBoxInput.x=Input.GetAxis("GearX");
        gearBoxInput.y=Input.GetAxis("GearY");
        // paddle input
        throttleInput=MathUtil.Map01(Input.GetAxis("Throttle"),-1f,1f);
        brakeInput=MathUtil.Map01(Input.GetAxis("Brake"),-1f,1f);
        clutchInput=MathUtil.Map01(Input.GetAxis("Clutch"),-1f,1f);
        // steering
        steerInput=Input.GetAxis("Horizontal");
    }
}