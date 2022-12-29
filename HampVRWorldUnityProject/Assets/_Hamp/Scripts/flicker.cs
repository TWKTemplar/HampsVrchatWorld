
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class flicker : UdonSharpBehaviour
{

    public Light fire;
    private float intensityMin = 0.5f;
    private float intensityMax = 1.5f;
    private float frequency = 0.1f;
    private float amplitude = 0.3f;

    void Update()
    {
        float t = Time.time;
        fire.intensity = Mathf.Lerp(intensityMin, intensityMax, Mathf.PerlinNoise(t * frequency, 0)) + Random.Range(-amplitude, amplitude);
    }


    //{

    //    public Light fire;


    //    void Update()

    //    {
    //        var x = Time.time;
    //        fire.intensity = (Mathf.Sin(x) * 0.25f + 1f) + (Mathf.Sin(x * 5) * 0.5f + 0.5f); 


    //    }

    //}

}
