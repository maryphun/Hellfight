using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMoveBehaviour : StateMachineBehaviour
{
    [SerializeField] float lastExitPoint = 0.0f;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo info, int layerIndex)
    {
        if (animator.GetBool("IsWalking")) return;

        animator.SetBool("IsWalking", true);
        //animator.Play(info.shortNameHash, layerIndex, lastExitPoint);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo info, int layerIndex)
    {
        //Debug.Log("Exit");
        lastExitPoint = info.normalizedTime;
        animator.SetBool("IsWalking", false);
    }
}
