
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]

public class simplespin : UdonSharpBehaviour
{
    public float speed;
    public RectTransform thingToSpin;

    private void FixedUpdate()
    {
        thingToSpin.Rotate(Vector3.forward, speed);
    }
}
