
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class BlurPostProcessingToggle : UdonSharpBehaviour
{

    public GameObject BlurEffectObject;
    public Image color;
    Color red = new Color(1, 0, 0);
    Color green = new Color(0, 1, 0);
    void Start()
    {
        if (BlurEffectObject == null) return;
        if (BlurEffectObject.activeInHierarchy)
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
        if (BlurEffectObject == null) return;
        if (BlurEffectObject.activeInHierarchy)
        {
            color.color = red;
            BlurEffectObject.SetActive(false);
        }
        else
        {
            color.color = green;
            BlurEffectObject.SetActive(true);

        }
    }
}
