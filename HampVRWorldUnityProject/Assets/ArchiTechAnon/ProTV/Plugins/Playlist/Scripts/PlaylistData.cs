
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace ArchiTech
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(-1)]
    public class PlaylistData : UdonSharpBehaviour
    {
        
        [HideInInspector] public VRCUrl[] urls;
        [HideInInspector] public VRCUrl[] alts;
        [HideInInspector] public string[] titles;
        [HideInInspector] public string[] tags;
        [HideInInspector] public Sprite[] images;
        [HideInInspector] public int[] titlePreSort;
        
    }
}
