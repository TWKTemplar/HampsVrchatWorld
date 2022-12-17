
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class CampFire : UdonSharpBehaviour
{
    public Photo photo;
    public MeshRenderer meshRenderer;
    public void Start()
    {
        if (meshRenderer != null) meshRenderer.enabled = false;
    }
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Something entered the trigger");
        if(other != null)
        {
            if(other.gameObject.layer ==13)//is a pickup object
            {
                photo = other.GetComponent<Photo>();
                photo.RespawnMultiplayer();
            }
        }
    }
}
