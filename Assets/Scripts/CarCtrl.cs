using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CarCtrl : Singleton<CarCtrl>
{
    public float maxMotorTorque;
    public float baseThrottle;
    public float motorForce; // 0-1
    public float brakeForce; // 0-1
    public AnimationCurve brakeCurve;
    public float dragForce;
    public AnimationCurve dragCurve;
    public float accelCoef; // 0-1
    public float maxSteerAngle;
    public WheelCollider wheelFL, wheelFR, wheelRL, wheelRR;
    [Header("Gear")]
    public float reverseTorqueCoef;
    public float[] torqueCurveCoefs;
    public AnimationCurve[] torqueCurves; //torque curve relative to the speed
    public AnimationCurve reverseTorqueCurve;
    [Header("Clutch")]
    public AnimationCurve clutchLowFrqCurve;
    public AnimationCurve clutchHighFrqCurve;

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
        lastTorque=0f;
        torqueDir=1f;
        SetBrakeTorque(0);
    }

    void Update(){
        HandleGear();
    }
    void FixedUpdate(){
        HandleMotor();
        HandleSteering();
        HandleClutchMotor();
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
    float lastTorque, torqueDir;
    void HandleMotor() {
        float throttle = HandleBaseThrottle(CarInput.inst.throttleInput);  // W/S 或 上/下
        float brake = CarInput.inst.brakeInput;

        float gearTorque=GetTorque();

        throttle*=gearTorque*CarInput.inst.clutchInput;
        float accel=0;
        if(brake>0.001f){ //brake
            accel=brakeForce*brake*brakeCurve.Evaluate(spd)*torqueDir;
        } else if(GearCtrl.inst.Gear!=0){ //sliding with neutral gear
            accel=throttle*motorForce;
        }
        accel+=torqueDir*dragForce*dragCurve.Evaluate(spd);

        torque=2*torque-lastTorque+accelCoef*accel*Time.fixedDeltaTime*Time.fixedDeltaTime;
        torqueDir=Mathf.Sign(torque);
        lastTorque=torque;

        if(brake>0.001f || (GearCtrl.inst.Gear!=0 && CarInput.inst.clutchInput > .001f)) {
            SetMotorTorque(torque);
        }
    }
    void HandleGear(){
        curGear=GearCtrl.inst.Gear;
    }
    void HandleSteering()
    {
        wheelFL.steerAngle = CarInput.inst.steerInput*maxSteerAngle;
        wheelFR.steerAngle = CarInput.inst.steerInput*maxSteerAngle;
    }
    float lastClutch;
    void HandleClutchMotor()
    {
        float clutch=CarInput.inst.clutchInput;
        if (Mathf.Abs(lastClutch - clutch) > .001f) {
            GamepadMotor.SetMotorSpeed(this, clutchLowFrqCurve.Evaluate(clutch), clutchHighFrqCurve.Evaluate(clutch));
            lastClutch=clutch;
        }
    }
    float GetTorque(){
        if(GearCtrl.inst.Gear==0) return 0;
        if(GearCtrl.inst.Gear==-1) return -reverseTorqueCurve.Evaluate(spd)*reverseTorqueCoef;
        return torqueCurves[GearCtrl.inst.Gear-1].Evaluate(spd)*torqueCurveCoefs[GearCtrl.inst.Gear-1];
    }
    // t=[0,1]
    float HandleBaseThrottle(float t) {
        return baseThrottle+(1-baseThrottle)*t;
    }
}
