using UnityEngine;
using TMPro;

public class CarUI:Singleton<CarUI>{
    public TMP_Text speedText;
    public TMP_Text gearText;
    public TMP_Text rpmText;
    public ProgressBar motorBar;
    void FixedUpdate(){
        speedText.text=$"spd={CarCtrl.inst.spd}";
        gearText.text=$"gear={CarCtrl.inst.curGear}";
        rpmText.text=$"rpm={CarCtrl.inst.rpm}";
    }
}