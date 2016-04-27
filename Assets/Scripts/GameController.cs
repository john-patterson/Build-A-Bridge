using UnityEngine;
using System.Collections;
using System.Linq;

public class GameController : MonoBehaviour
{
    const string VictoryTag = "VictoryZone";
    const string DeathTag = "DeathZone";

    public GameObject GameOverUI;
    public AudioSource Ambient;
    public GameObject[] Feet;


    bool RestartInput()
    {
        return Input.GetKeyDown(KeyCode.Space);
    }

    void Update()
    {
        if (RestartInput())
            GameRestart();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == VictoryTag)
            GameWin();
        else if (other.tag == DeathTag)
            GameLost();
    }

    private void GameWin()
    {
        BlockOutsideStimuli();
        GameOverUI.SetActive(true);
    }

    private void GameLost()
    {
        BlockOutsideStimuli();
        GameOverUI.SetActive(true);
    }

    private void BlockOutsideStimuli()
    {
        Ambient.Stop();
        Ambient.mute = true;
        Feet.SelectMany(f => f.GetComponents<AudioSource>()).Each(a =>
        {
            a.Stop();
            a.mute = true;
        });
        GetComponents<RUISCamera>().Each(rc => rc.gameObject.SetActive(false));
    }

    private void GameRestart()
    {
        Application.LoadLevel(Application.loadedLevel);
    }
}
