using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomLocalPosition : MonoBehaviour
{
    [SerializeField, Range(0.0f, 5.0f)] float range_X = 0.0f;
    [SerializeField, Range(0.0f, 5.0f)] float range_Y = 0.0f;
    void Start()
    {
        if (!ReferenceEquals(transform, null))
        {
            transform.localPosition = new Vector3(Random.Range(-range_X, range_X), Random.Range(-range_Y, range_Y), transform.localPosition.z);
            Destroy(this);
            return;
        }
        Debug.Log("Transform not found! Please set reference for " + gameObject.name+ ".");
    }
}
