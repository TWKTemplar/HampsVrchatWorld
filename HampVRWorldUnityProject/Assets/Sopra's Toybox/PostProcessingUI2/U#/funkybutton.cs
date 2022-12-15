
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class funkybutton : UdonSharpBehaviour
{
    public RectTransform button;
    Vector3 scaledown = new Vector3(0.25f, 0.25f, 0.25f);
    Vector3 defaultscale = new Vector3(1, 1, 1);
    public override void Interact()
    {
        button.localScale = scaledown;
    }

    public override void PostLateUpdate()
    {
        if(button.localScale.x < defaultscale.x)
        {
            button.localScale = Vector3.Lerp(button.localScale, defaultscale, Time.deltaTime * 8);
        }
    }
}
