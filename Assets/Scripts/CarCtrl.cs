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
    public float finalDriveRatio;
    public float[] gearRatios;
    public float reverseGearRatio;
    public float minRpm, maxRpm;
    public float maxSpd; //max speed that the gear could provide full torque
    public float idleSpd;
    public float torqueCurveCoef;
    public AnimationCurve torqueCurve;

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

        float gearRatio=GetGearRatio();
        //calculate rpm
        if(GearCtrl.inst.Gear==0)
            rpm=Mathf.Clamp(gearRatio*minRpm, minRpm, maxRpm);
        else {
            float wheelRpm=wheelFL.rpm;
            rpm=wheelRpm*gearRatio;
            if (Mathf.Abs(rpm) > maxRpm && wheelRpm>.001f) {
                gearRatio=maxRpm/wheelRpm*Mathf.Sign(rpm)*Mathf.Exp(maxRpm-Mathf.Abs(rpm));
                rpm=maxRpm;
            } else if (Mathf.Abs(rpm) < minRpm) {
                //gearRatio=minRpm/wheelRpm*Mathf.Sign(rpm);
                gearRatio*=idleSpd;
                rpm=minRpm;
            }
            rpm=Mathf.Abs(rpm);
        }
        gearRatio*=torqueCurve.Evaluate((rpm-minRpm)/(maxRpm-minRpm))*torqueCurveCoef;
        CarUI.inst.motorBar.SetProgress(torqueCurve.Evaluate((rpm-minRpm)/(maxRpm-minRpm)));

        accel*=gearRatio*CarInput.inst.clutchInput;
        if(brake>0.001f){ //brake
            torque=Mathf.Lerp(torque, 0, brakeForce);
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
    float GetGearRatio(){
        if(GearCtrl.inst.Gear==0) return 0;
        if(GearCtrl.inst.Gear==-1) return reverseGearRatio*finalDriveRatio;
        return gearRatios[GearCtrl.inst.Gear-1]*finalDriveRatio;
    }
}
