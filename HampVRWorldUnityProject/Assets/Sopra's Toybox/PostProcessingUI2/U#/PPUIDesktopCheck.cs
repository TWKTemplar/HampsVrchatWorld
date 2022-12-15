
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]

public class PPUIDesktopCheck : UdonSharpBehaviour
{
    public GameObject[] objs;
    public GameObject warning;
    void Start()
    {
        if(Networking.LocalPlayer.IsUserInVR() == true)
        {
            foreach(GameObject o in objs)
            {
                o.SetActive(false);
            }
            warning.SetActive(true);
        }
    }
}
