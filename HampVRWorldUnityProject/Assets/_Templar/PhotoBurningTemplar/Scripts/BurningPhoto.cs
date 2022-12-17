
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class BurningPhoto : UdonSharpBehaviour
{
    public SkinnedMeshRenderer skinnedMeshRenderer;
    public GameObject Effects;

    public Animator animator;
    public void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (skinnedMeshRenderer == null) skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
    }
    public void StartBurning()
    {
        Effects.SetActive(true);
        animator.SetBool("IsBurning", true);
    }
    public void ResetPhoto()
    {
        Effects.SetActive(false);
        animator.SetBool("IsBurning", false);
    }
}
