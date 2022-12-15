
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]

public class PlayerDetectionProx : UdonSharpBehaviour
{
    public GameObject loading;
    public GameObject menu;
    public RectTransform menuT;
    float dist;
    public float maxDist;
    bool toggled;
    void Start()
    {
        Toggle(false);
    }

    public override void PostLateUpdate()
    {
        dist = Vector3.Distance(Networking.LocalPlayer.GetPosition(), menuT.position);
        if (dist >= maxDist && toggled == true)
        {
            Toggle(false);
        }
        else if(dist < maxDist && toggled == false)
        {
            Toggle(true);
        }
    }
    void Toggle(bool o)
    {
        loading.SetActive(!o);
        menu.SetActive(o);
        toggled = o;
    }
}
