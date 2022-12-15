using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;
using UnityEditor;
[System.Serializable]
public class PPE : MonoBehaviour
{
    public PostProcessVolume[] pp;
    public PostProcessVolume[] twoSidedPp;
    public Slider[] Slider;
    public GameObject[] togglable;
    [SerializeField]
    public float aoWeight;
    [SerializeField]
    public float bloomWeight;
    [SerializeField]
    public float caWeight;
    [SerializeField]
    public float vnWeight;
    [SerializeField]
    public float conWeight;
    [SerializeField]
    public float satWeight;
    [SerializeField]
    public float expWeight;
    [SerializeField]
    public float tempWeight;
    [SerializeField]
    public float tintWeight;
    [SerializeField]
    public bool aoMode;
    [SerializeField]
    public bool ccMode;
    [SerializeField]
    public bool sound;
    [SerializeField]
    public bool mbMode;

    /*private void Start()
    {
        aoWeight = pp[0].weight;

        bloomWeight = pp[2].weight;

        caWeight = pp[3].weight;

        vnWeight = pp[4].weight;

        sound = togglable[4].activeSelf;

        aoMode = togglable[0].activeSelf;

        ccMode = togglable[2].activeSelf;

        if (twoSidedPp[0].weight == 0)
        {
            conWeight = twoSidedPp[1].weight * -1;
        }
        else
        {
            conWeight = twoSidedPp[0].weight;
        }

        if (twoSidedPp[2].weight == 0)
        {
            satWeight = twoSidedPp[3].weight * -1;
        }
        else
        {
            satWeight = twoSidedPp[2].weight;
        }

        if (twoSidedPp[4].weight == 0)
        {
            expWeight = twoSidedPp[5].weight * -1;
        }
        else
        {
            expWeight = twoSidedPp[4].weight;
        }

        if (twoSidedPp[6].weight == 0)
        {
            tempWeight = twoSidedPp[7].weight * -1;
        }
        else
        {
            tempWeight = twoSidedPp[6].weight;
        }

        if (twoSidedPp[8].weight == 0)
        {
            tintWeight = twoSidedPp[9].weight * -1;
        }
        else
        {
            tintWeight = twoSidedPp[8].weight;
        }

        UpdatePP();
    }
    */
    public void UpdatePP()
    {
        //Motion blur?
        if (mbMode == false)
        {
            pp[5].weight = 0;
        }
        else
        {
            pp[5].weight = 1;
        }
        //sounds?
        togglable[4].SetActive(sound);

        //AO MSVO TO SAO
        togglable[0].SetActive(aoMode);
        togglable[1].SetActive(!aoMode);

        //CC FILMIC TO NEUTRAL
        togglable[2].SetActive(ccMode);
        togglable[3].SetActive(!ccMode);

        //ALL NORMAL WEIGHTS
        pp[0].weight = aoWeight;
        pp[1].weight = aoWeight;

        pp[2].weight = bloomWeight;

        pp[3].weight = caWeight;

        pp[4].weight = vnWeight;

        //FUCKED UP WEIGHTS
        if(conWeight >= 0)
        {
            twoSidedPp[0].weight = conWeight;
            twoSidedPp[1].weight = 0;
        }
        else
        {
            twoSidedPp[0].weight = 0;
            twoSidedPp[1].weight = Mathf.Abs(conWeight);
        }
        Slider[0].value = conWeight + 1;

        if (satWeight >= 0)
        {
            twoSidedPp[2].weight = satWeight;
            twoSidedPp[3].weight = 0;
        }
        else
        {
            twoSidedPp[2].weight = 0;
            twoSidedPp[3].weight = Mathf.Abs(satWeight);
        }
        Slider[1].value = satWeight + 1;

        if (expWeight >= 0)
        {
            twoSidedPp[4].weight = expWeight;
            twoSidedPp[5].weight = 0;
        }
        else
        {
            twoSidedPp[4].weight = 0;
            twoSidedPp[5].weight = Mathf.Abs(expWeight);
        }
        Slider[2].value = expWeight + 1;

        if (tempWeight >= 0)
        {
            twoSidedPp[6].weight = tempWeight;
            twoSidedPp[7].weight = 0;
        }
        else
        {
            twoSidedPp[6].weight = 0;
            twoSidedPp[7].weight = Mathf.Abs(tempWeight);
        }
        Slider[3].value = tempWeight + 1;

        if (tintWeight >= 0)
        {
            twoSidedPp[8].weight = tintWeight;
            twoSidedPp[9].weight = 0;
        }
        else
        {
            twoSidedPp[8].weight = 0;
            twoSidedPp[9].weight = Mathf.Abs(tintWeight);
        }
        Slider[4].value = tintWeight + 1;
    }
    
}
