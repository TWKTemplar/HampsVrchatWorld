
using UdonSharp;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]

public class sliderppUsharp : UdonSharpBehaviour
{
    [Header("If you change the weight, the slider will change ingame.")]
    public PostProcessVolume postp;
    public Slider sld;
    /*void Start()
    {
        sld.value = postp.weight;
    }*/

    public override void Interact()
    {
        postp.weight = sld.value;
    }
}
