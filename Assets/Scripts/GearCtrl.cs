using System;
using EdyCommonTools;
using TMPro;
using UnityEngine;

public class GearCtrl:Singleton<GearCtrl>{
    public float scale;
    public float gearTriggerDist;
    public Transform stick;

    public ProgressBar throttleBar, brakeBar, clutchBar;

    // gear index:
    // 1   3   5
    // |   |   |
    // 0---0---0
    // |   |   |
    // 2   4   -1
    int gear, lastGear;
    StickStage stickStage, lastStickStage;
    Vector3 anchorML, anchorMR, anchorTL, anchorTR, anchorBL, anchorBR, anchorTM, anchorBM;
    Vector3 stickCenter;
    StickOffset stickOffset;
    bool joyStickAtCenter;

    //debug
    public TMP_Text textStickStage;
    public TMP_Text textGear, textLastGear;
    public TMP_Text textStickOffset;
    //debug
    void OnValidate(){
        if(inst==null) inst=this;
        stickCenter=stick.position;
        stickOffset=new StickOffset(StickStage.Center);
        CalculateAnchorPos();
    }
    void OnDrawGizmosSelected(){
        Gizmos.DrawLine(anchorMR, anchorML);
        Gizmos.DrawLine(anchorBM, anchorTM);
        Gizmos.DrawLine(anchorBL, anchorTL);
        Gizmos.DrawLine(anchorBR, anchorTR);
        Gizmos.DrawWireSphere(stickCenter, gearTriggerDist);
    }
    void Start(){
        OnValidate();
    }
    void FixedUpdate(){
        throttleBar.SetProgress(CarInput.inst.throttleInput);
        brakeBar.SetProgress(CarInput.inst.brakeInput);
        clutchBar.SetProgress(CarInput.inst.clutchInput);
        UpdateStickPos(CarInput.inst.gearBoxInput);
        UpdateGear();
        textGear.text=$"gear: {gear}";
        textLastGear.text=$"lastGear: {lastGear}";
        textStickOffset.text=$"stickOffset: {stickOffset.origin}";
    }
    void CalculateAnchorPos(){
        anchorML=stickCenter;
        anchorML.x-=scale;
        anchorMR=stickCenter;
        anchorMR.x+=scale;
        anchorTL=anchorML;
        anchorTL.y+=scale;
        anchorBL=anchorML;
        anchorBL.y-=scale;
        anchorTR=anchorMR;
        anchorTR.y+=scale;
        anchorBR=anchorMR;
        anchorBR.y-=scale;
        anchorBM=stickCenter;
        anchorBM.y-=scale;
        anchorTM=stickCenter;
        anchorTM.y+=scale;
    }
    void UpdateJoyStickStage(Vector2 input) {
        joyStickAtCenter=Vector2.SqrMagnitude(input)<.01f;
    }
    // input is the input from the joy stick. assume it is normalized
    // 3   5   8
    // |   |   |
    // 2---0---7
    // |   |   |
    // 1   4   6
    // for this gear, [n] means the [n]th digit that is 1 in that enum
    // then each edge's enum value equals to [n|m], given two stick position enum [n] and [m]
    enum StickStage
    {
        Center=0b1,
        BL=0b10,
        ML=0b100,
        TL=0b1000,
        BR=0b1000000,
        MR=0b10000000,
        TR=0b100000000,
        TM=0b100000,
        BM=0b10000,
        // dangling
        Center2L=Center|ML,
        Center2R=Center|MR,
        Center2T=Center|TM,
        Center2B=Center|BM,
        ML2T=ML|TL,
        ML2B=ML|BL,
        MR2T=MR|TR,
        MR2B=MR|BR,
        Dangling=0,
    }
    class StickDist : IComparable<StickDist>{
        public float dist;
        public StickStage stage;
        public StickDist(float dist, StickStage stage) {
            this.dist=dist;
            this.stage=stage;
        }
        public int CompareTo(StickDist other) {
            if(other==null) return 0;
            if(dist<other.dist) return -1;
            else if(dist>other.dist) return 1;
            return 0;
        }
    }
    class StickOffset
    {
        public Vector3 value;
        public StickStage origin;
        public StickOffset(StickStage origin) {
            SetOrigin(origin);
        }
        public void SetOrigin(StickStage origin) {
            this.origin=origin;
            switch (origin) {
                case StickStage.Center:
                    value=GearCtrl.inst.stickCenter;
                    break;
                case StickStage.TL:
                    value=GearCtrl.inst.anchorTL;
                    break;
                case StickStage.ML:
                    value=GearCtrl.inst.anchorML;
                    break;
                case StickStage.BL:
                    value=GearCtrl.inst.anchorBL;
                    break;
                case StickStage.TR:
                    value=GearCtrl.inst.anchorTR;
                    break;
                case StickStage.MR:
                    value=GearCtrl.inst.anchorMR;
                    break;
                case StickStage.BR:
                    value=GearCtrl.inst.anchorBR;
                    break;
                case StickStage.TM:
                    value=GearCtrl.inst.anchorTM;
                    break;
                case StickStage.BM:
                    value=GearCtrl.inst.anchorBM;
                    break;
            }
        }
    }
    StickStage GetStickStage(Vector3 stickPos)
    {
        float sqr_trigger_dist=gearTriggerDist*gearTriggerDist;
        StickDist[] arr=new StickDist[9] {
            new StickDist(Vector3.SqrMagnitude(stickPos-stickCenter), StickStage.Center),
            new StickDist(Vector3.SqrMagnitude(stickPos-anchorTL), StickStage.TL),
            new StickDist(Vector3.SqrMagnitude(stickPos-anchorML), StickStage.ML),
            new StickDist(Vector3.SqrMagnitude(stickPos-anchorBL), StickStage.BL),
            new StickDist(Vector3.SqrMagnitude(stickPos-anchorTR), StickStage.TR),
            new StickDist(Vector3.SqrMagnitude(stickPos-anchorMR), StickStage.MR),
            new StickDist(Vector3.SqrMagnitude(stickPos-anchorBR), StickStage.BR),
            new StickDist(Vector3.SqrMagnitude(stickPos-anchorTM), StickStage.TM),
            new StickDist(Vector3.SqrMagnitude(stickPos-anchorBM), StickStage.BM),
        };
        Array.Sort(arr);
        if(arr[0].dist<sqr_trigger_dist) return arr[0].stage;
        return arr[0].stage|arr[1].stage;
    }
    void UpdateStickPos(Vector2 input){
        Vector3 scaledInput=(Vector3)(input*scale);
        Vector3 stickPos=stick.position-stickCenter;
        Vector3 targetPos=stickPos;
        //get stick stage
        StickStage tmpStickStage=stickStage;
        stickStage=GetStickStage(stick.position);
        //update stick position
        switch (stickStage) {
            //Center
            case StickStage.Center:
                if(joyStickAtCenter) stickOffset.SetOrigin(StickStage.Center);
                switch (stickOffset.origin) {
                    case StickStage.Center:
                        targetPos=scaledInput;
                        if(Mathf.Abs(targetPos.x)>Mathf.Abs(targetPos.y))
                            targetPos.y=0;
                        else targetPos.x=0;
                        break;
                    case StickStage.TM:
                        if(Mathf.Abs(input.x)>Mathf.Abs(input.y+.5f))
                            targetPos.x=scaledInput.x;
                        else targetPos.y=scaledInput.y*2;
                        break;
                }
                break;
            //Center edge
            case StickStage.Center2L:
            case StickStage.Center2R:
                targetPos.x=scaledInput.x;
                break;
            case StickStage.Center2T:
                targetPos.y=scaledInput.y;
                break;
            case StickStage.Center2B:
                targetPos.y=scaledInput.y;
                break;
            //Left
            case StickStage.ML:
                targetPos=scaledInput;
                if(Mathf.Abs(scale+targetPos.x)>Mathf.Abs(targetPos.y))
                    targetPos.y=0;
                break;
            case StickStage.TL:
                targetPos.y=Mathf.Clamp(scaledInput.y,-scale,0f)+scale;
                break;
            case StickStage.BL:
                targetPos.y=Mathf.Clamp(scaledInput.y, 0f, scale)-scale;
                break;
            case StickStage.ML2T:
            case StickStage.ML2B:
                targetPos.x=-scale;
                if(lastGear==0)
                    targetPos.y=scaledInput.y;
                break;
            //Right
            case StickStage.MR:
                targetPos=scaledInput;
                if(Mathf.Abs(scale-targetPos.x)>Mathf.Abs(targetPos.y))
                    targetPos.y=0;
                break;
            case StickStage.TR:
                targetPos.y=Mathf.Clamp(scaledInput.y,-scale,0f)+scale;
                break;
            case StickStage.BR:
                targetPos.y=Mathf.Clamp(scaledInput.y, 0f, scale)-scale;
                break;
            case StickStage.MR2T:
            case StickStage.MR2B:
                targetPos.x=scale;
                if(lastGear==0)
                    targetPos.y=scaledInput.y;
                break;
            //Middle Top
            case StickStage.TM:
                if(stickOffset.origin!=StickStage.TM)
                    stickOffset.SetOrigin(StickStage.TM);
                switch (stickOffset.origin)
                {
                    case StickStage.TM:
                        targetPos.y=Mathf.Clamp(scaledInput.y, -scale, 0f)*2;
                        break;
                    default:
                        targetPos.y=scaledInput.y*2-scale;
                        break;
                }
                break;
            //Middle Bottom
            case StickStage.BM:
                targetPos.y=Mathf.Clamp(scaledInput.y, 0f, scale)-scale;
                break;
        }
        stick.position=targetPos+stickOffset.value;
        if(tmpStickStage!=stickStage)
            lastStickStage=tmpStickStage;
        textStickStage.text=stickStage.ToString();
    }
    void UpdateGear()
    {
        int tmpGear=gear;
        switch (stickStage) {
            case StickStage.TL:
                gear=1;
                break;
            case StickStage.BL:
                gear=2;
                break;
            case StickStage.TM:
                gear=3;
                break;
            case StickStage.BM:
                gear=4;
                break;
            case StickStage.TR:
                gear=5;
                break;
            case StickStage.BR:
                gear=-1;
                break;
            default:
                gear=0;
                break;
        }
        //update lastGear only if the gear has changed
        if(tmpGear!=gear || lastStickStage!=stickStage)
            lastGear=tmpGear;
    }
}