using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarCtrl : Singleton<CarCtrl>
{
    public float maxMotorTorque;
    public float motorForce;
    public float brakeForce; // 0-1
    public float dragForce; // 0-1
    public float maxSteerAngle;
    public WheelCollider wheelFL, wheelFR, wheelRL, wheelRR;
    [Header("Gear")]
    public int gearCount;
    public float maxSpd; //max speed that the gear could provide full torque

    [HideInInspector] public Rigidbody rgb;

    [HideInInspector] public int curGear;

    [HideInInspector] public float spd;
    [HideInInspector] public float torque;
    protected override void Awake(){
        base.Awake();
        rgb=GetComponent<Rigidbody>();
        curGear=0;
    }
    // Start is called before the first frame update
    void Start()
    {
        torque=0f;
        SetBrakeTorque(0);
    }

    void Update(){
        HandleGear();
    }
    void FixedUpdate(){
        HandleMotor();
        HandleSteering();
        spd=rgb.velocity.magnitude;
    }
    void SetBrakeTorque(float f){
        wheelFL.brakeTorque = f;
        wheelFR.brakeTorque = f;
        wheelRL.brakeTorque = f;
        wheelRR.brakeTorque = f;
    }
    void SetMotorTorque(float f){
        wheelFL.motorTorque = f;
        wheelFR.motorTorque = f;
        wheelRL.motorTorque = f;
        wheelRR.motorTorque = f;
    }
    void HandleMotor() {
        float accel = CarInput.inst.throttleInput;  // W/S 或 上/下
        float brake = CarInput.inst.brakeInput;

        float gearMotorCoef=GetGearMotorCoef();
        accel*=gearMotorCoef;
        if(brake>0.001f){ //brake
            torque=Mathf.Lerp(torque, 0, brakeForce);
        } else if(Mathf.Abs(accel)<0.001f){ //sliding
            torque=Mathf.Lerp(torque, 0, dragForce);
        } else{ //accelerating
            accel*=maxMotorTorque;
            torque=Mathf.Lerp(torque, accel, motorForce);
        }

        SetMotorTorque(torque);
    }
    void HandleGear(){
        curGear=GearCtrl.inst.Gear;
    }
    void HandleSteering()
    {
        wheelFL.steerAngle = CarInput.inst.steerInput*maxSteerAngle;
        wheelFR.steerAngle = CarInput.inst.steerInput*maxSteerAngle;
    }
    float GetGearMotorCoef(){
        float spd_range_min=maxSpd*curGear/gearCount;
        float spd_range_max=maxSpd*(curGear+1)/gearCount;

        // get the coefficient between 0-1 regardless of the curGear
        float normalizedCoef;
        if(spd<spd_range_min)
            normalizedCoef=Mathf.Exp(spd-spd_range_min);
        else if(spd>spd_range_max)
            normalizedCoef=Mathf.Exp(-(spd-spd_range_max));
        else
            normalizedCoef=1;
        CarUI.inst.motorBar.SetProgress(normalizedCoef);
        // calculate the actual coef
        normalizedCoef*=(float)(curGear+1)/gearCount;
        return normalizedCoef;
    }
}
