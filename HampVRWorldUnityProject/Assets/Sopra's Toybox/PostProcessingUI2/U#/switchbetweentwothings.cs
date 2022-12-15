
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class switchbetweentwothings : UdonSharpBehaviour
{
    public GameObject one;
    public GameObject two;
    public Color state1;
    public Color state2;
    public Image graphic;
    void Start()
    {
        if(one.activeSelf == true)
        {
            graphic.color = state1;
        }
        else
        {
            graphic.color = state2;
        }
    }

    public override void Interact()
    {
        if (one.activeSelf == true)
        {
            one.SetActive(false);
            two.SetActive(true);
            graphic.color = state2;
        }
        else
        {
            one.SetActive(true);
            two.SetActive(false);
            graphic.color = state1;
        }
    }
}
