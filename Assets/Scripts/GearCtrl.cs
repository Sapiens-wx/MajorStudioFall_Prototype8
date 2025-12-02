using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class GearCtrl:Singleton<GearCtrl>{
    public float scale;
    public float gearTriggerDist;
    public float stickToNeutralEps, maxStickDisplacement;
    public RectTransform stick;

    public ProgressBar throttleBar, brakeBar, clutchBar;
    [Header("Vibration")]
    public float vibrateLowFrq;
    public float vibrateHighFrq;
    public float vibrateDuration;

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
    Coroutine vibrateCoro;

    public int Gear {
        get=>gear;
    }

    void OnValidate(){
        if(inst==null) inst=this;
        stickCenter=stick.anchoredPosition; //changed to anchor pos instead of world pos
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
        UpdateStickPos(CarInput.inst.gearBoxInput, CarInput.inst.gearBoxInputDelta);
        UpdateGear();
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
    // row-wise: top(2^0) middle(2^1) bottom(2^2)
    // col-wise: left(2^3) middle(2^4) right(2^5)
    // to describe a position, we use [row][col].
    // 
    // then each edge's enum value equals to [n|m], given two stick position enum [n] and [m]
    enum StickStage
    {
        Center=RowMiddle|ColMiddle,
        BL=RowBottom|ColLeft,
        ML=RowMiddle|ColLeft,
        TL=RowTop|ColLeft,
        BR=RowBottom|ColRight,
        MR=RowMiddle|ColRight,
        TR=RowTop|ColRight,
        TM=RowTop|ColMiddle,
        BM=RowBottom|ColMiddle,
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
        // not used for a stage or position
        RowTop=0b1,
        RowMiddle=0b10,
        RowBottom=0b100,
        ColLeft=0b1000,
        ColMiddle=0b10000,
        ColRight=0b100000,
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
            SetOrigin(origin, false);
        }
        public void SetOrigin(StickStage origin, bool duplicateDetection) {
            if(this.origin==origin && duplicateDetection) return;
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
    void UpdateStickPos(Vector2 input, Vector2 inputDelta){
        float maxSpd=maxStickDisplacement*scale;
        /*Vector3 scaledInput=(Vector3)(input*scale);
        Vector3 stickPos=stick.position-stickCenter;
        Vector3 targetPos=stickPos;
        //get stick stage
        StickStage tmpStickStage=stickStage;
        stickStage = GetStickStage(stick.position);
        */

        Vector3 scaledInput = (Vector3)(input * scale);

        // world pos -> anchor pos
        Vector3 stickPos = (Vector3)stick.anchoredPosition - stickCenter;
        Vector3 targetPos = stickPos;

        StickStage tmpStickStage = stickStage;
        stickStage = GetStickStage(stick.anchoredPosition);


        
        //update stick position
        switch (stickStage) {
            //Center
            case StickStage.Center:
                if(joyStickAtCenter) stickOffset.SetOrigin(StickStage.Center, true);
                switch (stickOffset.origin) {
                    case StickStage.Center:
                        targetPos=scaledInput;
                        if(Mathf.Abs(targetPos.x)>Mathf.Abs(targetPos.y))
                            targetPos.y=0;
                        else targetPos.x=0;
                        break;
                    case StickStage.TM:
                        targetPos.y=-scale;
                        targetPos.x=0;
                        if(Mathf.Abs(input.x)>Mathf.Abs(input.y+.5f))
                            targetPos.x=scaledInput.x;
                        else targetPos.y=Mathf.Clamp(scaledInput.y, -scale, 0)*2;
                        break;
                    case StickStage.BM:
                        targetPos.y=scale;
                        targetPos.x=0;
                        if(Mathf.Abs(input.x)>Mathf.Abs(input.y-.5f))
                            targetPos.x=scaledInput.x;
                        else targetPos.y=Mathf.Clamp(scaledInput.y, 0, scale)*2;
                        break;
                    case StickStage.TL:
                        targetPos.x=scale;
                        targetPos.y=-scale;
                        if(Mathf.Abs(input.x-.5f)>Mathf.Abs(input.y+.5f))
                            targetPos.x=scaledInput.x*2;
                        else targetPos.y=Mathf.Clamp(scaledInput.y, -scale, 0)*2;
                        break;
                    case StickStage.BL:
                        targetPos.x=scale;
                        targetPos.y=scale;
                        if(Mathf.Abs(input.x-.5f)>Mathf.Abs(input.y-.5f))
                            targetPos.x=scaledInput.x*2;
                        else targetPos.y=Mathf.Clamp(scaledInput.y, 0, scale)*2;
                        break;
                    case StickStage.TR:
                        targetPos.x=-scale;
                        targetPos.y=-scale;
                        if(Mathf.Abs(input.x+.5f)>Mathf.Abs(input.y+.5f))
                            targetPos.x=scaledInput.x*2;
                        else targetPos.y=Mathf.Clamp(scaledInput.y, -scale, 0)*2;
                        break;
                    case StickStage.BR:
                        targetPos.x=-scale;
                        targetPos.y=scale;
                        if(Mathf.Abs(input.x+.5f)>Mathf.Abs(input.y-.5f))
                            targetPos.x=scaledInput.x*2;
                        else targetPos.y=Mathf.Clamp(scaledInput.y, 0, scale)*2;
                        break;
                }
                break;
            //Center edge
            case StickStage.Center2L:
            case StickStage.Center2R:
                switch (stickOffset.origin) {
                    case StickStage.Center:
                        targetPos.x=scaledInput.x;
                        targetPos.y=0;
                        break;
                    case StickStage.TM:
                        targetPos.x=scaledInput.x;
                        targetPos.y=-scale;
                        if((inputDelta.x<stickToNeutralEps && stickStage==StickStage.Center2R)||
                            inputDelta.x>stickToNeutralEps && stickStage==StickStage.Center2L){
                            stickOffset.SetOrigin(StickStage.Center, false);
                            targetPos=Vector2.zero;
                        }
                        break;
                    case StickStage.BM:
                        targetPos.x=scaledInput.x;
                        targetPos.y=scale;
                        if((inputDelta.x<stickToNeutralEps && stickStage==StickStage.Center2R)||
                            inputDelta.x>stickToNeutralEps && stickStage==StickStage.Center2L){
                            stickOffset.SetOrigin(StickStage.Center, false);
                            targetPos=Vector2.zero;
                        }
                        break;
                    case StickStage.TL:
                        targetPos.y=-scale;
                        targetPos.x=scaledInput.x*2;
                        if (inputDelta.x < -stickToNeutralEps) {
                            targetPos=Vector2.zero;
                            stickOffset.SetOrigin(StickStage.Center, false);
                        }
                        break;
                    case StickStage.BL:
                        targetPos.y=scale;
                        targetPos.x=scaledInput.x*2;
                        if (inputDelta.x < -stickToNeutralEps) {
                            targetPos=Vector2.zero;
                            stickOffset.SetOrigin(StickStage.Center, false);
                        }
                        break;
                    case StickStage.TR:
                        targetPos.y=-scale;
                        targetPos.x=scaledInput.x*2;
                        if (inputDelta.x > stickToNeutralEps) {
                            targetPos=Vector2.zero;
                            stickOffset.SetOrigin(StickStage.Center, false);
                        }
                        break;
                    case StickStage.BR:
                        targetPos.y=scale;
                        targetPos.x=scaledInput.x*2;
                        if (inputDelta.x > stickToNeutralEps) {
                            targetPos=Vector2.zero;
                            stickOffset.SetOrigin(StickStage.Center, false);
                        }
                        break;
                }
                break;
            case StickStage.Center2T:
            case StickStage.Center2B:
                switch (stickOffset.origin) {
                    case StickStage.Center:
                        targetPos.x=0;
                        targetPos.y=scaledInput.y;
                        break;
                    case StickStage.TM:
                        targetPos.x=0;
                        targetPos.y=scaledInput.y*2;
                        if(inputDelta.y>stickToNeutralEps){
                            stickOffset.SetOrigin(StickStage.Center, false);
                            targetPos=Vector2.zero;
                        }
                        break;
                    case StickStage.BM:
                        targetPos.x=0;
                        targetPos.y=scaledInput.y*2;
                        if(inputDelta.y<stickToNeutralEps){
                            stickOffset.SetOrigin(StickStage.Center, false);
                            targetPos=Vector2.zero;
                        }
                        break;
                    case StickStage.TL:
                        targetPos.x=scale;
                        targetPos.y=scaledInput.y*2;
                        break;
                    case StickStage.BL:
                        targetPos.x=scale;
                        targetPos.y=scaledInput.y*2;
                        break;
                    case StickStage.TR:
                        targetPos.x=-scale;
                        targetPos.y=scaledInput.y*2;
                        break;
                    case StickStage.BR:
                        targetPos.x=-scale;
                        targetPos.y=scaledInput.y*2;
                        break;
                }
                break;
            //Left
            case StickStage.ML:
                switch (stickOffset.origin) {
                    case StickStage.Center:
                        targetPos=scaledInput;
                        if(Mathf.Abs(scale+targetPos.x)>Mathf.Abs(targetPos.y))
                            targetPos.y=0;
                        break;
                    case StickStage.TM:
                        if (Mathf.Abs(input.x + 1f) > Mathf.Abs(input.y*2 + 1f)) { //x axis move
                            targetPos.x=scaledInput.x;
                            targetPos.y=-scale;
                        } else { //y axis move
                            targetPos.x=-scale;
                            targetPos.y=Mathf.Clamp(scaledInput.y, -scale, 0)*2;
                        }
                        break;
                    case StickStage.BM:
                        if (Mathf.Abs(input.x + 1f) > Mathf.Abs(input.y*2 - 1f)) { //x axis move
                            targetPos.x=scaledInput.x;
                            targetPos.y=scale;
                        } else { //y axis move
                            targetPos.x=-scale;
                            targetPos.y=Mathf.Clamp(scaledInput.y, 0, scale)*2;
                        }
                        break;
                    case StickStage.TL:
                        if(Mathf.Abs(input.x)>Mathf.Abs(input.y*2+1f)){ //x axis move
                            targetPos.x=Mathf.Clamp(scaledInput.x, 0f, scale);
                            targetPos.y=-scale;
                        } else { //y axis move
                            targetPos.x=0;
                            targetPos.y=Mathf.Clamp(scaledInput.y, -scale, 0)*2;
                        }
                        break;
                    case StickStage.BL:
                        if(Mathf.Abs(input.x)>Mathf.Abs(input.y*2-1f)){ //x axis move
                            targetPos.x=Mathf.Clamp(scaledInput.x, 0f, scale);
                            targetPos.y=scale;
                        } else { //y axis move
                            targetPos.x=0;
                            targetPos.y=Mathf.Clamp(scaledInput.y, 0, scale)*2;
                        }
                        break;
                    case StickStage.TR:
                        if(Mathf.Abs(input.x+1f)>Mathf.Abs(input.y*2+1f)){ //x axis move
                            targetPos.x=Mathf.Clamp(scaledInput.x, -scale, 0)*2;
                            targetPos.y=-scale;
                        } else{ //y axis move
                            targetPos.x=-scale*2;
                            targetPos.y=Mathf.Clamp(scaledInput.y, -scale, 0)*2;
                        }
                        break;
                    case StickStage.BR:
                        if(Mathf.Abs(input.x+1f)>Mathf.Abs(input.y*2-1f)){ //x axis move
                            targetPos.x=Mathf.Clamp(scaledInput.x, -scale, 0)*2;
                            targetPos.y=scale;
                        } else{ //y axis move
                            targetPos.x=-scale*2;
                            targetPos.y=Mathf.Clamp(scaledInput.y, 0, scale)*2;
                        }
                        break;
                }
                break;
            case StickStage.TL:
                stickOffset.SetOrigin(StickStage.TL, true);
                switch (stickOffset.origin) {
                    case StickStage.TL:
                        targetPos.y=Mathf.Clamp(scaledInput.y, -scale, 0)*2;
                        targetPos.x=0;
                        break;
                }
                break;
            case StickStage.BL:
                stickOffset.SetOrigin(StickStage.BL, true);
                switch (stickOffset.origin) {
                    case StickStage.BL:
                        targetPos.y=Mathf.Clamp(scaledInput.y, 0f, scale)*2;
                        targetPos.x=0;
                        break;
                }
                break;
            case StickStage.ML2T:
            case StickStage.ML2B:
                switch (stickOffset.origin) {
                    case StickStage.Center:
                        targetPos.x=-scale;
                        targetPos.y=scaledInput.y;
                        break;
                    case StickStage.TM:
                    case StickStage.BM:
                        targetPos.x=-scale;
                        targetPos.y=scaledInput.y*2;
                        if((inputDelta.y<-stickToNeutralEps&&stickOffset.origin==StickStage.BM)||
                            (inputDelta.y > stickToNeutralEps && stickOffset.origin == StickStage.TM)) {
                            stickOffset.SetOrigin(StickStage.Center, false);
                            targetPos=Vector2.zero;
                        }
                        break;
                    case StickStage.TL:
                    case StickStage.BL:
                        targetPos.x=0;
                        targetPos.y=scaledInput.y*2;
                        if((inputDelta.y<-stickToNeutralEps&&stickOffset.origin==StickStage.BL)||
                            (inputDelta.y > stickToNeutralEps && stickOffset.origin == StickStage.TL)) {
                            stickOffset.SetOrigin(StickStage.Center, false);
                            targetPos=Vector2.zero;
                        }
                        break;
                    case StickStage.TR:
                    case StickStage.BR:
                        targetPos.x=-scale*2;
                        targetPos.y=scaledInput.y*2;
                        if((inputDelta.y<-stickToNeutralEps&&stickOffset.origin==StickStage.TR)||
                            (inputDelta.y > stickToNeutralEps && stickOffset.origin == StickStage.BR)) {
                            stickOffset.SetOrigin(StickStage.Center, false);
                            targetPos=Vector2.zero;
                        }
                        break;
                }
                break;
            //Right
            case StickStage.MR:
                switch (stickOffset.origin) {
                    case StickStage.Center:
                        targetPos=scaledInput;
                        if(Mathf.Abs(input.x-1f)>Mathf.Abs(input.y))
                            targetPos.y=0;
                        break;
                    case StickStage.TM:
                        if (Mathf.Abs(input.x - 1f) > Mathf.Abs(input.y*2 + 1f)) { //x axis move
                            targetPos.x=scaledInput.x;
                            targetPos.y=-scale;
                        } else { //y axis move
                            targetPos.x=scale;
                            targetPos.y=Mathf.Clamp(scaledInput.y, -scale, 0)*2;
                        }
                        break;
                    case StickStage.BM:
                        if (Mathf.Abs(input.x - 1f) > Mathf.Abs(input.y*2 - 1f)) { //x axis move
                            targetPos.x=scaledInput.x;
                            targetPos.y=scale;
                        } else { //y axis move
                            targetPos.x=scale;
                            targetPos.y=Mathf.Clamp(scaledInput.y, 0, scale)*2;
                        }
                        break;
                    case StickStage.TL:
                        if(Mathf.Abs(input.x-1)>Mathf.Abs(input.y*2+1f)){ //x axis move
                            targetPos.x=Mathf.Clamp(scaledInput.x, 0f, scale)*2;
                            targetPos.y=-scale;
                        } else { //y axis move
                            targetPos.x=scale*2;
                            targetPos.y=Mathf.Clamp(scaledInput.y, -scale, 0)*2;
                        }
                        break;
                    case StickStage.BL:
                        if(Mathf.Abs(input.x-1)>Mathf.Abs(input.y*2-1f)){ //x axis move
                            targetPos.x=Mathf.Clamp(scaledInput.x, 0f, scale)*2;
                            targetPos.y=scale;
                        } else { //y axis move
                            targetPos.x=scale*2;
                            targetPos.y=Mathf.Clamp(scaledInput.y, 0, scale)*2;
                        }
                        break;
                    case StickStage.TR:
                        if(Mathf.Abs(input.x)>Mathf.Abs(input.y*2+1f)){ //x axis move
                            targetPos.x=Mathf.Clamp(scaledInput.x, -scale, 0)*2;
                            targetPos.y=-scale;
                        } else{ //y axis move
                            targetPos.x=0;
                            targetPos.y=Mathf.Clamp(scaledInput.y, -scale, 0)*2;
                        }
                        break;
                    case StickStage.BR:
                        if(Mathf.Abs(input.x)>Mathf.Abs(input.y*2-1f)){ //x axis move
                            targetPos.x=Mathf.Clamp(scaledInput.x, -scale, 0)*2;
                            targetPos.y=scale;
                        } else{ //y axis move
                            targetPos.x=0;
                            targetPos.y=Mathf.Clamp(scaledInput.y, 0, scale)*2;
                        }
                        break;
                }
                break;
            case StickStage.TR:
                stickOffset.SetOrigin(StickStage.TR, true);
                switch (stickOffset.origin) {
                    case StickStage.TR:
                        targetPos.y=Mathf.Clamp(scaledInput.y, -scale, 0)*2;
                        targetPos.x=0;
                        break;
                }
                break;
            case StickStage.BR:
                stickOffset.SetOrigin(StickStage.BR, true);
                switch (stickOffset.origin) {
                    case StickStage.BR:
                        targetPos.y=Mathf.Clamp(scaledInput.y, 0, scale)*2;
                        targetPos.x=0;
                        break;
                }
                break;
            case StickStage.MR2T:
            case StickStage.MR2B:
                switch (stickOffset.origin) {
                    case StickStage.Center:
                        targetPos.x=scale;
                        targetPos.y=scaledInput.y;
                        break;
                    case StickStage.TM:
                    case StickStage.BM:
                        targetPos.x=scale;
                        targetPos.y=scaledInput.y*2;
                        if((inputDelta.y>stickToNeutralEps&&stickOffset.origin==StickStage.TM)||
                            (inputDelta.y < -stickToNeutralEps && stickOffset.origin == StickStage.BM)) {
                            stickOffset.SetOrigin(StickStage.Center, false);
                            targetPos=Vector2.zero;
                        }
                        break;
                    case StickStage.TL:
                    case StickStage.BL:
                        targetPos.x=scale*2;
                        targetPos.y=scaledInput.y*2;
                        if((inputDelta.y>stickToNeutralEps&&stickOffset.origin==StickStage.TL)||
                            (inputDelta.y < -stickToNeutralEps && stickOffset.origin == StickStage.BL)) {
                            stickOffset.SetOrigin(StickStage.Center, false);
                            targetPos=Vector2.zero;
                        }
                        break;
                    case StickStage.TR:
                    case StickStage.BR:
                        targetPos.x=0;
                        targetPos.y=scaledInput.y*2;
                        if((inputDelta.y>stickToNeutralEps&&stickOffset.origin==StickStage.TR)||
                            (inputDelta.y < -stickToNeutralEps && stickOffset.origin == StickStage.BR)) {
                            stickOffset.SetOrigin(StickStage.Center, false);
                            targetPos=Vector2.zero;
                        }
                        break;
                }
                break;
            //Middle Top
            case StickStage.TM:
                if(stickOffset.origin!=StickStage.TM)
                    stickOffset.SetOrigin(StickStage.TM, true);
                switch (stickOffset.origin)
                {
                    case StickStage.TM:
                        targetPos.y=Mathf.Clamp(scaledInput.y, -scale, 0f)*2;
                        break;
                }
                break;
            //Middle Bottom
            case StickStage.BM:
                if(stickOffset.origin!=StickStage.BM)
                    stickOffset.SetOrigin(StickStage.BM, true);
                switch (stickOffset.origin)
                {
                    case StickStage.BM:
                        targetPos.y=scaledInput.y*2;
                        break;
                }
                break;
        }
        // clamp targetPos based on the offset
        switch (stickOffset.origin) {
            case StickStage.TL:
            case StickStage.TM:
            case StickStage.TR:
                targetPos.y=Mathf.Clamp(targetPos.y, -2*scale, 0);
                break;
            case StickStage.BL:
            case StickStage.BM:
            case StickStage.BR:
                targetPos.y=Mathf.Clamp(targetPos.y, 0, 2*scale);
                break;
        }
        //update stick position
        /*Vector3 actualPosOffset=targetPos+stickOffset.value-stick.position;
        float actualPosOffsetMag=actualPosOffset.magnitude;
        if(actualPosOffsetMag>maxSpd)
            actualPosOffset=actualPosOffset/actualPosOffsetMag*maxSpd;
        stick.position+=actualPosOffset;*/

        Vector3 currentAnchored = stick.anchoredPosition;
        Vector3 actualPosOffset = targetPos + stickOffset.value - currentAnchored;

        float actualPosOffsetMag = actualPosOffset.magnitude;
        if (actualPosOffsetMag > maxSpd)
            actualPosOffset = actualPosOffset / actualPosOffsetMag * maxSpd;
       
        stick.anchoredPosition = (Vector2)(currentAnchored + actualPosOffset);

        if (tmpStickStage!=stickStage)
            lastStickStage=tmpStickStage;
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
        if(tmpGear!=gear) {
            lastGear=tmpGear;
            if(vibrateCoro!=null)
                StopCoroutine(vibrateCoro);
            vibrateCoro=StartCoroutine(Vibrate());
        }
    }
    IEnumerator Vibrate()
    {
        if (CarCtrl.inst.EngineState == CarCtrl.EEngineState.On) {
            GamepadMotor.SetMotorSpeed(this, vibrateLowFrq, vibrateHighFrq);
            yield return new WaitForSeconds(vibrateDuration);
            GamepadMotor.SetMotorSpeed(this, 0f, 0f);
        }
        vibrateCoro=null;
    }
}