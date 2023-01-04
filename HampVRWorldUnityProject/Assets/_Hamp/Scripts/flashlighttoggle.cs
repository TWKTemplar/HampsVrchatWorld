
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class flashlighttoggle : UdonSharpBehaviour
    
{
    public GameObject flashlight;
    public AudioSource audioSource;
    public override void OnPickupUseDown()
    {
        ToggleFlashLight();
    }
    public void ToggleFlashLight()
    {
        if(audioSource != null) audioSource.Play();

        if (flashlight.activeInHierarchy)
        {
            //TurnOffFlashlight();
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "TurnOffFlashlight");
        }
        else
        {
            //TurnOnFlashlight();
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "TurnOnFlashlight");
        }
    }
    public void TurnOnFlashlight()
    {
        flashlight.SetActive(true);
    }
    public void TurnOffFlashlight()
    {
        flashlight.SetActive(false);
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        if (Networking.IsMaster)
        {
            if (flashlight.activeInHierarchy)
            {   
                //TurnOnFlashlight();
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "TurnOnFlashlight");
            }
            else
            {
                //TurnOffFlashlight();
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "TurnOffFlashlight");
            }
        }
    }
}
