using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetailChecker : MonoBehaviour
{
    Dictionary<string, string> detail;
    public void InsertDetail(Dictionary<string, string> _detail)
    {
        detail = _detail;
    }

    public void CheckDetail()
    {
        FindObjectOfType<Leaderboard>().CheckDetailOpen(detail);
    }
}
