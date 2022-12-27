
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class hqmirror : UdonSharpBehaviour

{
    public GameObject hqMirror;
    public GameObject bestMirror;
    

    void Start()
    {
    
    }

    public override void Interact()
    {
        hqMirror.SetActive(!hqMirror.activeInHierarchy);
        bestMirror.SetActive(false);
    }
}
