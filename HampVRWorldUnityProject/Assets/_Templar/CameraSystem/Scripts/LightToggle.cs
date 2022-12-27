
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class LightToggle : UdonSharpBehaviour
{
    public SkinnedMeshRenderer rend;
    public GameObject Light;
    public bool IsLightsOn = true;
    public void Start()
    {
        SyncAnimWithLight();
    }
    public override void Interact()
    {
        IsLightsOn = !IsLightsOn;//Flip the bool
        if (IsLightsOn)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetLightsOn");
        }
        else
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetLightsOff");
        }
    }
    public void SetLightsOn()
    {
        IsLightsOn = true;
        Light.SetActive(IsLightsOn);
        SyncAnimWithLight();
    }
    public void SetLightsOff()
    {
        IsLightsOn = false;
        Light.SetActive(IsLightsOn);
        SyncAnimWithLight();
    }
    public void SyncAnimWithLight()
    {
        if (!IsLightsOn)
        {
            rend.SetBlendShapeWeight(1, 100);
        }
        else
        {
            rend.SetBlendShapeWeight(1, 0);
        }
    }
}
