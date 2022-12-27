
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class CameraPickUp : UdonSharpBehaviour
{
    public Animator anim;
    public VRC_Pickup pickup;
    public SkinnedMeshRenderer myRend;
    public VRCPlayerApi localplayer;
    public float ImageBlendValue = 0;
    public float ImageBlendSpeed = 10;
    public float TimeSinceLastPhotoTaken = 0f;

    public float TimeSinceLastPhotoTakenCoolDown = 5f;
    public void Start()
    {
        localplayer = Networking.LocalPlayer;
    }
    public void Update()
    {
        TimeSinceLastPhotoTaken += Time.deltaTime;
        LookAtCameraBack();
        
    }
    public void LookAtCameraBack()
    {
        var dist = Vector3.Distance(localplayer.GetBonePosition(HumanBodyBones.Head), transform.position);
        if(TimeSinceLastPhotoTaken < TimeSinceLastPhotoTakenCoolDown)
        {
            if(pickup.IsHeld) ImageBlendValue += Time.deltaTime * ImageBlendSpeed;
        }
        else
        {
            ImageBlendValue -= Time.deltaTime * ImageBlendSpeed;
        }
        ImageBlendValue = Mathf.Clamp(ImageBlendValue, 0, 100);
        myRend.SetBlendShapeWeight(0, ImageBlendValue);// 0 to 100
    }
    public override void OnPickupUseDown()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LocalTakePicture");
    }
    public void LocalTakePicture()
    {
        TimeSinceLastPhotoTaken = 0;
        anim.Play("TakePic");
    }

}
