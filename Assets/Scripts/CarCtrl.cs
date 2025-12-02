using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CarCtrl : Singleton<CarCtrl>
{
    public float baseThrottle;
    public float motorForce; // 0-1
    public float brakeForce; // 0-1
    public AnimationCurve brakeCurve;
    public float dragForce;
    public AnimationCurve dragCurve;
    public float accelCoef; // 0-1
    public float maxSteerAngle;
    public WheelCollider wheelFL, wheelFR, wheelRL, wheelRR;
    [Header("Rpm")]
    public float minRpm;
    public float maxRpm;
    public AnimationCurve[] rpmCurves;
    [Header("Gear")]
    public float reverseTorqueCoef;
    public float[] torqueCurveCoefs;
    public AnimationCurve[] torqueCurves; //torque curve relative to the speed
    public AnimationCurve reverseTorqueCurve;
    [Header("Clutch")]
    public AnimationCurve clutchLowFrqCurve;
    public AnimationCurve clutchHighFrqCurve;
    [Header("Jumping")]
    public float jumpForce;
    public float jumpTorque;
    public float jumpCoolDown;
    public float jumpAngleDeg;
    [Header("Engine")]
    public float engineStartDuration;
    public float engineLowFrq, engineHighFrq;
    public AudioSource engineAudioSource;
    public AudioClip enginStartClip, engineOnClip;

    [HideInInspector] public Rigidbody rgb;

    [HideInInspector] public int curGear;

    [HideInInspector] public float spd;
    [HideInInspector] public float torque, maxTorque;
    [HideInInspector] public float rpm;
    EEngineState engineState;
    public EEngineState EngineState {
        get=>engineState;
        set {
            if(engineState==value) return;
            engineState=value;
            if(engineState==EEngineState.Off){
                rpm=0;
                engineButtonDownTime=Time.time;
            } else if(engineState==EEngineState.On) rpm=minRpm;
        }
    }
    public enum EEngineState {
        Off,
        Starting,
        On
    }

    float lastJumpTime;
    protected override void Awake(){
        base.Awake();
        rgb=GetComponent<Rigidbody>();
        curGear=0;
        lastJumpTime=-jumpCoolDown;
        EngineState=EEngineState.Off;
    }
    // Start is called before the first frame update
    void Start()
    {
        torque=0f;
        lastTorque=0f;
        torqueDir=1f;
        SetBrakeTorque(0);
    }

    void FixedUpdate(){
        HandleEngine();
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
        if(rpm<minRpm && EngineState==EEngineState.On) EngineState=EEngineState.Off;
        float throttle = HandleBaseThrottle(CarInput.inst.throttleInput);  // W/S 或 上/下
        float brake = CarInput.inst.brakeInput;

        float accel=0;
        if (brake > 0.001f) { // brake
            accel=brakeForce*brake*brakeCurve.Evaluate(spd)*torqueDir;
        }
        if(engineState==EEngineState.On) {
            float gearTorque=GetTorque();
            if (GearCtrl.inst.Gear == 5) {
                if(throttle>.3f) Jump();
            } else {
                throttle*=gearTorque*CarInput.inst.clutchInput;
                if(brake>0.001f){ //brake
                } else if(GearCtrl.inst.Gear!=0){ //sliding with neutral gear
                    accel=throttle*motorForce;
                }
                if(GearCtrl.inst.Gear!=0&&GearCtrl.inst.Gear!=5)
                    accel+=torqueDir*dragForce*dragCurve.Evaluate(rpm/maxRpm)*Mathf.Atan(spd)*CarInput.inst.clutchInput;
            }

            // Rpm
            if (GearCtrl.inst.Gear == 0 || GearCtrl.inst.Gear == 5) {
                rpm=Mathf.Lerp(minRpm, maxRpm, CarInput.inst.throttleInput);
            } else {
                int gear=GearCtrl.inst.Gear;
                if(gear==-1) gear=1;
                rpm=Mathf.Lerp(0, maxRpm, rpmCurves[gear-1].Evaluate(spd));
                rpm=Mathf.Lerp(Mathf.Lerp(minRpm, maxRpm, CarInput.inst.throttleInput), rpm, CarInput.inst.clutchInput);
            }
        }
        torque=2*torque-lastTorque+accelCoef*accel*Time.fixedDeltaTime*Time.fixedDeltaTime;
        lastTorque=torque;
        if(brake>0.001f || ((GearCtrl.inst.Gear!=0&&GearCtrl.inst.Gear!=5) && CarInput.inst.clutchInput > .001f)) {
            torqueDir=Mathf.Sign(torque);
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
        if (engineState==EEngineState.On) {
            float clutch=CarInput.inst.clutchInput;
            if (Mathf.Abs(lastClutch - clutch) > .001f) {
                GamepadMotor.SetMotorSpeed(this, clutchLowFrqCurve.Evaluate(clutch), clutchHighFrqCurve.Evaluate(clutch));
                lastClutch=clutch;
            }
        }
    }
    float GetTorque(){
        if(GearCtrl.inst.Gear==0||GearCtrl.inst.Gear==5) return 0;
        if(GearCtrl.inst.Gear==-1) return -reverseTorqueCurve.Evaluate(spd)*reverseTorqueCoef;
        return torqueCurves[GearCtrl.inst.Gear-1].Evaluate(spd)*torqueCurveCoefs[GearCtrl.inst.Gear-1];
    }
    // t=[0,1]
    float HandleBaseThrottle(float t) {
        return baseThrottle+(1-baseThrottle)*t;
    }
    float engineButtonDownTime;
    void HandleEngine() {
        if(engineState!=EEngineState.On) {
            if (CarInput.inst.engineInput == false) { //stops starting the engine
                EngineState=EEngineState.Off;
                engineButtonDownTime=Time.time;
                rpm=0f;
                GamepadMotor.SetMotorSpeed(this, 0f, 0f);
                if(engineAudioSource.isPlaying){
                    engineAudioSource.Stop();
                    engineAudioSource.clip=null;
                }
            } else if(Time.time-engineButtonDownTime>engineStartDuration){
                EngineState=EEngineState.On;
                GamepadMotor.SetMotorSpeed(this, 0f, 0f);
                engineAudioSource.clip=engineOnClip;
                engineAudioSource.loop=true;
                engineAudioSource.Play();
            } else {
                float t=Time.time-engineButtonDownTime;
                t/=engineStartDuration;
                rpm=Random.Range(0, minRpm*.75f);
                if(EngineState!=EEngineState.Starting){
                    GamepadMotor.SetMotorSpeed(this, engineLowFrq*t, engineHighFrq*t);
                    EngineState=EEngineState.Starting;
                    engineAudioSource.clip=enginStartClip;
                    engineAudioSource.loop=false;
                    engineAudioSource.Play();
                }
            }
        }
    }
    void Jump() {
        if (Time.time - lastJumpTime >= jumpCoolDown) {
            lastJumpTime=Time.time;
            Vector3 jumpDir=MathUtil.RandomDirection(jumpAngleDeg);
            rgb.AddForce(jumpDir*jumpForce);
            Vector3 torqueDir=-CarInput.inst.steerInputVec2.x*transform.right+CarInput.inst.steerInputVec2.y*transform.forward;
            float torqueCoef=torqueDir.magnitude;
            if (torqueCoef > .05f) {
                torqueDir/=torqueCoef; //normalize
                torqueDir=Vector3.Cross(torqueDir, transform.up);
                rgb.AddTorque(torqueDir*torqueCoef*jumpTorque);
            }
        }
    }
}
