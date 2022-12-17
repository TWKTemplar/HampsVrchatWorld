
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Photo : UdonSharpBehaviour
{
    public Transform OriginalPosition;
    public VRCPlayerApi localplayer;
    public VRC_Pickup myPickUp;
    public MeshRenderer meshRenderer;
    public BurningPhoto BurningPhoto;
    public float BurnTime = 15;
    public GameObject Effects;
    void Start()
    {
        if(OriginalPosition == null) OriginalPosition = GetComponentInParent<Transform>();
        localplayer = Networking.LocalPlayer;
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
    }
    public void DoBurn()
    {
        Debug.Log("Burning");
        myPickUp.Drop();
        if (localplayer.isMaster)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ActivateBurningVersion");
            SendCustomEventDelayedSeconds("ResetObject", BurnTime);
        }   
    }
    public void ResetObject()//resets everything
    {
        Networking.SetOwner(localplayer, gameObject);
        gameObject.transform.position = OriginalPosition.transform.position;
        meshRenderer.enabled = true;
        BurningPhoto.skinnedMeshRenderer.enabled = false;
        BurningPhoto.ResetPhoto();
        Effects.SetActive(false);
    }
    public void ActivateBurningVersion()
    {
        meshRenderer.enabled = false;
        BurningPhoto.skinnedMeshRenderer.enabled = true;
        BurningPhoto.skinnedMeshRenderer.materials = meshRenderer.materials;
        BurningPhoto.StartBurning();
        Effects.SetActive(true);
    }



}
