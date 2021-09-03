using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Effect : MonoBehaviour
{
    [SerializeField] float removeTime = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        Animator anim;
        anim = GetComponent<Animator>();
        GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        if (removeTime == 0.0f)
        {
            Destroy(gameObject, anim.GetCurrentAnimatorClipInfo(0)[0].clip.length);
        }
        else
        {
            Destroy(gameObject, removeTime);
        }
    }
}
