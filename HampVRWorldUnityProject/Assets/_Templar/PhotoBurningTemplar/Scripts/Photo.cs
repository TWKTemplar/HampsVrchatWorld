
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Photo : UdonSharpBehaviour
{
    public Transform OriginalPosition;
    public VRCPlayerApi localplayer;
    public VRC_Pickup myPickUp;
    public float BurnTime = 15;
    public GameObject Effects;

    public Animator animator;
    void Start()
    {
        if(OriginalPosition == null) OriginalPosition = GetComponentInParent<Transform>();
        localplayer = Networking.LocalPlayer;
        if (animator == null) animator = GetComponent<Animator>();
    }
    public void DoBurn()
    {
        myPickUp.Drop();
        if (localplayer.isMaster)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "StartBurningAnim");
            SendCustomEventDelayedSeconds("ResetObject", BurnTime);
        }   
    }
    public void ResetObject()//resets everything
    {
        myPickUp.Drop();
        Networking.SetOwner(localplayer, gameObject);
        gameObject.transform.position = OriginalPosition.transform.position;
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "StopBurningAnim");
    }
    public void StartBurningAnim()
    {
        if (Effects != null) Effects.SetActive(true);
        if (animator != null) animator.SetBool("IsBurning", true);
    }
    public void StopBurningAnim()
    {
        if (Effects != null) Effects.SetActive(false);
        if (animator != null) animator.SetBool("IsBurning", false);
    }



}
