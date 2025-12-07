using UnityEngine;

public class Pedal : MonoBehaviour {
    public enum EPedalType {
        Throttle,
        Brake,
        Clutch
    }
    public EPedalType pedalType;
    public Transform pedalTransform;
    public Vector3 rotAxis;
    public float rotAmount;

    Quaternion initRot;
    void Start() {
        initRot=pedalTransform.localRotation;
        rotAxis.Normalize();
    }
    void FixedUpdate() {
        float pedalInput=0f;
        switch(pedalType){
            case EPedalType.Throttle:
                pedalInput=CarInput.inst.throttleInput;
                break;
            case EPedalType.Brake:
                pedalInput=CarInput.inst.brakeInput;
                break;
            case EPedalType.Clutch:
                pedalInput=1-CarInput.inst.clutchInput;
                break;
        }
        pedalTransform.localRotation=initRot*Quaternion.AngleAxis(pedalInput*rotAmount, rotAxis);
    }
}