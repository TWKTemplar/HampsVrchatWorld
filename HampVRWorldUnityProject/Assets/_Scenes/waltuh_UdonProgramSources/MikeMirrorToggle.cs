
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class MirrorToggle : UdonSharpBehaviour
{
    public GameObject bestMirror;
    public GameObject hqMirror;
    //public bool mirrorIsOn;

    void Start()
    {

    }
    //
    //public override void Interact()
    //{
    //    if (mirrorIsOn == false)
    //    {
    //        bestMirror.SetActive(true);
    //        mirrorIsOn = true;
    //    }
    //    else
    //    {
    //        bestMirror.SetActive(false);
    //        mirrorIsOn = false;
    //    }
    //}
    public override void Interact()
    {
     bestMirror.SetActive(!bestMirror.activeInHierarchy);
        hqMirror.SetActive(false);

    }
}
