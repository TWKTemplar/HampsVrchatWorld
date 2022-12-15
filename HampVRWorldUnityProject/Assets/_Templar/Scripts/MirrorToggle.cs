
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class MirrorToggle : UdonSharpBehaviour
{
    public GameObject Mirror;
    public override void Interact()
    {
        Mirror.SetActive(!Mirror.activeSelf);
    }
}
