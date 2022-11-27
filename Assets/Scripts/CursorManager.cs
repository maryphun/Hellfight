using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private float hideTimeThreshold = 2.5f;

    private Coroutine co_HideCursor;

    void Update()
    {
        if (Input.GetAxis("Mouse X") == 0 && (Input.GetAxis("Mouse Y") == 0))
        {
            if (co_HideCursor == null)
            {
                co_HideCursor = StartCoroutine(HideCursor());
            }
        }
        else
        {
            if (co_HideCursor != null)
            {
                StopCoroutine(co_HideCursor);
                co_HideCursor = null;
                Cursor.visible = true;
            }
        }
    }

    private IEnumerator HideCursor()
    {
        yield return new WaitForSeconds(hideTimeThreshold);
        Cursor.visible = false;
    }
}
