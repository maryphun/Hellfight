using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewGroundAPI : MonoBehaviour
{
    [SerializeField] private io.newgrounds.core ngio_core;
    [SerializeField] private bool EnableNewGroundsAPI = false;

    public io.newgrounds.core GetNewGroundsAPI()
    {
        return ngio_core;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (!EnableNewGroundsAPI) return;
        ngio_core.onReady(() =>
        {
            ngio_core.checkLogin((bool logged_in) =>
            {
                if (logged_in)
                {
                    onLoggedIn();
                }
                else
                {
                    requestLogin();
                }
            });
        });
    }

    private void onLoggedIn()
    {
        if (!EnableNewGroundsAPI) return;
        io.newgrounds.objects.user player = ngio_core.current_user;
    }

    private void requestLogin()
    {
        if (!EnableNewGroundsAPI) return;
        ngio_core.requestLogin(onLoggedIn, onLoginFailed, onLoginCancelled);
    }

    private void onLoginFailed()
    {
        if (!EnableNewGroundsAPI) return;

    }

    private void onLoginCancelled()
    {
        if (!EnableNewGroundsAPI) return;

    }

    public void NGUnlockMedal(int medal_id)
    {
        if (!EnableNewGroundsAPI) return;
        Debug.Log("medalunlock");
        io.newgrounds.SessionResult tmp = new io.newgrounds.SessionResult();
        if (tmp.session.user != null)
        {
            Debug.Log("medal unlock success! id: " + medal_id);
            io.newgrounds.components.Medal.unlock medal_unlock = new io.newgrounds.components.Medal.unlock();

            medal_unlock.id = medal_id;

            medal_unlock.callWith(ngio_core);
        }
    }

    public void NGSubmitScore(int score_id, int score)
    {
        if (!EnableNewGroundsAPI) return;
        Debug.Log("Check Newgrounds connect status");
        io.newgrounds.SessionResult tmp = new io.newgrounds.SessionResult();
        if (tmp.session.user != null)
        {
            Debug.Log("is connected. upload score " + score.ToString() + " to score board " + score_id.ToString());
            io.newgrounds.components.ScoreBoard.postScore submit_score = new io.newgrounds.components.ScoreBoard.postScore();
            submit_score.id = score_id;
            submit_score.value = score;
            submit_score.callWith(ngio_core);
        }
    }
}
