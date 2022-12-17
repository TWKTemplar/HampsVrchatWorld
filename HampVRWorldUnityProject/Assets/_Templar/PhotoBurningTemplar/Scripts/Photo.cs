
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Photo : UdonSharpBehaviour
{
    public Transform OriginalPosition;
    public VRCPlayerApi localplayer;
    public VRC_Pickup myPickUp;
    void Start()
    {
        if(OriginalPosition == null) OriginalPosition = GetComponentInParent<Transform>();
        localplayer = Networking.LocalPlayer;
    }
    public void RespawnMultiplayer()
    {
        Debug.Log("Respawning");
        myPickUp.Drop();
        if (localplayer.isMaster)
        {
            Networking.SetOwner(localplayer, gameObject);
            gameObject.transform.position = OriginalPosition.transform.position;
        }   
    }
    


}
