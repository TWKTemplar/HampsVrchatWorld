
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Teleport : UdonSharpBehaviour
{
    private VRCPlayerApi localplayer;
    [Range(0.1f, 2)] public float TeleportDistance;
    public GameObject TeleportPoint;
    public void Start()
    {
        localplayer = Networking.LocalPlayer;
    }
    public void Update()
    {
        //Finding the distance from the script to the local player
        var dist = Vector3.Distance(localplayer.GetPosition(), transform.position);
        if(dist < TeleportDistance)
        {
            localplayer.TeleportTo(TeleportPoint.transform.position, TeleportPoint.transform.rotation);
        }
    }
}
