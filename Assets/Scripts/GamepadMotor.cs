using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine.InputSystem;

public class GamepadMotor : Singleton<GamepadMotor>
{
    List<MotorInfo> list;
    class MotorInfo
    {
        public object obj;
        public float lowFrq, highFrq;
        public MotorInfo(object obj, float lowFrq, float highFrq) {
            this.obj=obj;
            this.lowFrq=lowFrq;
            this.highFrq=highFrq;
        }
    }
    protected override void Awake() {
        base.Awake();
        list=new List<MotorInfo>();
    }
    void OnDisable()
    {
        if(Gamepad.current!=null)
            Gamepad.current.SetMotorSpeeds(0f, 0f);
    }
    MotorInfo FindInList(object obj) {
        foreach(var e in list) {
            if(e.obj==obj) return e;
        }
        return null;
    }
    public static void SetMotorSpeed(object obj, float lowFrq, float highFrq)
    {
        float maxLowFrq=0, maxHighFrq=0;
        var motorSpd4Obj=inst.FindInList(obj);
        if (motorSpd4Obj != null) {
            motorSpd4Obj.lowFrq=lowFrq;
            motorSpd4Obj.highFrq=highFrq;
        } else {
            inst.list.Add(new MotorInfo(obj, lowFrq, highFrq));
        }
        foreach(var e in inst.list) {
            maxLowFrq=Mathf.Max(e.lowFrq, maxLowFrq);
            maxHighFrq=Mathf.Max(e.highFrq, maxHighFrq);
        }
        if(Gamepad.current!=null)
            Gamepad.current.SetMotorSpeeds(maxLowFrq, maxHighFrq);
    }

}