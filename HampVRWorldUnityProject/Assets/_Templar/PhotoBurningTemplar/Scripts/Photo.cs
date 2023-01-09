
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
    public bool RespawnAfterBurn = true;
    public Animator animator;
    public bool CoolDownOnFireRemoval = false;
    public float LiveBurningTime = -100;
    void Start()
    {
        if(OriginalPosition == null) OriginalPosition = GetComponentInParent<Transform>();
        localplayer = Networking.LocalPlayer;
        if (animator == null) animator = GetComponent<Animator>();
    }
    public void Update()
    {
        if (localplayer.isMaster)
        {
            if(LiveBurningTime > 0)
            {
                LiveBurningTime -= Time.deltaTime;
            }
            else
            {
                if(LiveBurningTime > -99)
                {
                    LiveBurningTime = -100;
                    ResetObject();
                }
            }
                
        }
    }
    #region Toggle Gravity
    public void RbSetIsKinamaticTrueNetworkCall()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RbSetIsKinamaticTrueLocal");
        RbSetIsKinamaticTrueLocal();
    }
    public void RbSetIsKinamaticFalseNetworkCall()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RbSetIsKinamaticFalseLocal");
        RbSetIsKinamaticFalseLocal();
    }
    public void RbSetIsKinamaticTrueLocal()
    {
        GetComponent<Rigidbody>().isKinematic = true;
    }
    public void RbSetIsKinamaticFalseLocal()
    {
        GetComponent<Rigidbody>().isKinematic = false;
    }
    #endregion
    public void DoBurn()
    {
        myPickUp.Drop();
        if (localplayer.isMaster)
        {
            RbSetIsKinamaticFalseNetworkCall();//Turns on gravity
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "StartBurningAnim");
            if (CoolDownOnFireRemoval == false) LiveBurningTime = BurnTime;
        }   
    }
    public void ResetObject()//resets everything(Networked)
    {
        LiveBurningTime = -100;
        if (RespawnAfterBurn)
        {
            myPickUp.Drop();
            Networking.SetOwner(localplayer, gameObject);
            gameObject.transform.position = OriginalPosition.transform.position;
        }
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "StopBurningAnim");
    }
    public void CoolDown()
    {
        if (localplayer.isMaster)
        {
            if (CoolDownOnFireRemoval == true)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "StopBurningAnim");
            }
        }
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
