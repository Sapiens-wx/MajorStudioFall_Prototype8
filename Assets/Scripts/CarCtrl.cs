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
    public float reverseTorqueCoef;
    public float[] torqueCurveCoefs;
    public AnimationCurve[] torqueCurves; //torque curve relative to the speed
    public AnimationCurve reverseTorqueCurve;

    [HideInInspector] public Rigidbody rgb;

    [HideInInspector] public int curGear;

    [HideInInspector] public float spd;
    [HideInInspector] public float torque;
    [HideInInspector] public float rpm;
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

        float gearTorque=GetTorque();

        accel*=gearTorque*CarInput.inst.clutchInput;
        if(brake>0.001f){ //brake
            torque=Mathf.Lerp(torque, 0, brakeForce*brake);
        } else if(Mathf.Abs(accel)<0.001f){ //sliding
            torque=Mathf.Lerp(torque, 0, dragForce);
        } else{ //accelerating
            accel*=maxMotorTorque;
            torque=Mathf.Lerp(torque, accel, motorForce);
        }

        if(brake>0.001f || (GearCtrl.inst.Gear!=0 && CarInput.inst.clutchInput>.001f))
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
    float GetTorque(){
        if(GearCtrl.inst.Gear==0) return 0;
        if(GearCtrl.inst.Gear==-1) return -reverseTorqueCurve.Evaluate(spd)*reverseTorqueCoef;
        return torqueCurves[GearCtrl.inst.Gear-1].Evaluate(spd)*torqueCurveCoefs[GearCtrl.inst.Gear-1];
    }
}
