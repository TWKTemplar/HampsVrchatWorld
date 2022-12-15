
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class TwoSidedSliderMapper : UdonSharpBehaviour
{
    [Header("If you change the weight, the slider will change ingame.")]
    public PostProcessVolume[] postp;
    public Slider sld;
    float sldval;
    /*void Start()
    {
        SetPPVal();
    }*/

    public override void Interact()
    {
        SetPPVal();
    }

    void SetPPVal()
    {
        sldval = sld.value - 1;
        if(sldval >= 0)
        {
            postp[0].weight = sldval;
            postp[1].weight = 0;
        }
        else
        {
            postp[0].weight = 0;
            postp[1].weight = Mathf.Abs(sldval);
        }
    }
}
