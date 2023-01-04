
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class flashlighttoggle : UdonSharpBehaviour
    
{
    public GameObject flashlight;
    public override void OnPickupUseDown()
    {
        flashlight.SetActive(!flashlight.activeInHierarchy);
    }
    void Start()
    {
        
    }
}
