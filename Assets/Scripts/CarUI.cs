using UnityEngine;
using TMPro;

public class CarUI:Singleton<CarUI>{
    public TMP_Text speedText;
    public TMP_Text gearText;
    public ProgressBar motorBar;
    void FixedUpdate(){
        speedText.text=$"spd={CarCtrl.inst.spd}";
        gearText.text=$"gear={CarCtrl.inst.curGear}";
        //motorBar.SetProgress(CarCtrl.inst.torque/CarCtrl.inst.maxMotorTorque);
    }
}