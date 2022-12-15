
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class onofftogglemb : UdonSharpBehaviour
{
    public PostProcessVolume pp;
    public Image color;
    Color red = new Color(1, 0, 0);
    Color green = new Color(0, 1, 0);
    void Start()
    {
        if(pp.weight > 0)
        {
            color.color = green;
        }
        else
        {
            color.color = red;
        }
    }

    public override void Interact()
    {
        if (pp.weight > 0)
        {
            color.color = red;
            pp.weight = 0;
        }
        else
        {
            color.color = green;
            pp.weight = 1;
        }
    }
}
