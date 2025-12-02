using UnityEngine;
using System;

public class Steering : Singleton<Steering>
{
    public Transform steerGameObject;
    public float maxSteerObjAngle;
    public float maxSteerAngle; //used to normalize steer input
    [HideInInspector][NonSerialized] public float steer; //normalized [-1,1]
    bool wasSteering;
    int steerDir; //left -1, right 1, none 0
    Vector2 lastDir;
    void Start() {
        wasSteering=false;
    }
    void FixedUpdate() {
        HandleSteering();
        HandleDisplay();
    }
    void HandleSteering() {
        Vector2 input=CarInput.inst.steerInputVec2;
        float magnitude=input.magnitude;
        if (magnitude > .3f) {
            Vector2 dir=input/magnitude;
            if (wasSteering == false) {
                wasSteering=true;
            } else {
                float dAngle=Vector2.SignedAngle(lastDir, dir);
                steer+=dAngle/maxSteerAngle;
                steer=Mathf.Clamp(steer, -1f, 1f);
            }
            lastDir=dir;
        } else {
            wasSteering=false;
            steer=Mathf.Lerp(steer, 0f, .1f);
        }
    }
    void HandleDisplay() {
        steerGameObject.localRotation=Quaternion.Euler(0f, -steer*maxSteerObjAngle, 0f);
    }
}